using System.Globalization;
using System.Reflection;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.DynamicCrud.Import;

/// <summary>
/// Excel/CSV dosyasını parse edip entity'lere dönüştürür ve toplu import yapar.
/// </summary>
public interface IDynamicImportService
{
    /// <summary>Dosyayı parse edip önizleme ve otomatik kolon eşleştirme döner.</summary>
    Task<ImportPreview> ParseFileAsync(string entityName, Stream fileStream, string fileType, CancellationToken ct = default);

    /// <summary>Dosyayı import eder (kullanıcının onayladığı kolon eşleştirmesiyle).</summary>
    Task<ImportResult> ImportAsync(string entityName, Stream fileStream, string fileType,
        Dictionary<int, string> columnMapping, CancellationToken ct = default);
}

public sealed class DynamicImportService : IDynamicImportService
{
    private readonly IDynamicEntityRegistry _registry;
    private readonly IDynamicDbContextProvider _dbContextProvider;
    private readonly IMetadataService _metadataService;
    private readonly ILogger<DynamicImportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DynamicImportService(
        IDynamicEntityRegistry registry,
        IDynamicDbContextProvider dbContextProvider,
        IMetadataService metadataService,
        ILogger<DynamicImportService> logger)
    {
        _registry = registry;
        _dbContextProvider = dbContextProvider;
        _metadataService = metadataService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  PARSE FILE (Preview + Auto Mapping)
    // ═══════════════════════════════════════════════════════════

    public async Task<ImportPreview> ParseFileAsync(
        string entityName, Stream fileStream, string fileType, CancellationToken ct = default)
    {
        var metadata = _metadataService.GetMetadata(entityName)
            ?? throw new KeyNotFoundException($"Entity '{entityName}' not found.");

        var editableFields = metadata.Fields
            .Where(f => !f.ReadOnly && string.IsNullOrEmpty(f.Computed))
            .ToList();

        List<string> fileHeaders;
        List<List<string>> allRows;

        if (fileType.Contains("csv", StringComparison.OrdinalIgnoreCase))
        {
            (fileHeaders, allRows) = await ParseCsvAsync(fileStream, ct);
        }
        else
        {
            (fileHeaders, allRows) = ParseExcel(fileStream);
        }

        // Otomatik eşleştirme
        var suggestedMapping = ColumnMapper.AutoMap(fileHeaders, editableFields);

        // İlk 10 satır preview
        var previewRows = allRows.Take(10).ToList();

        return new ImportPreview
        {
            FileHeaders = fileHeaders,
            EntityFields = editableFields.Select(f => new ImportFieldInfo
            {
                Name = f.Name,
                Label = f.Label,
                Type = f.Type,
                Required = f.Required
            }).ToList(),
            SuggestedMapping = suggestedMapping,
            PreviewRows = previewRows,
            TotalRowCount = allRows.Count
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  IMPORT
    // ═══════════════════════════════════════════════════════════

    public async Task<ImportResult> ImportAsync(
        string entityName, Stream fileStream, string fileType,
        Dictionary<int, string> columnMapping, CancellationToken ct = default)
    {
        var metadata = _metadataService.GetMetadata(entityName)
            ?? throw new KeyNotFoundException($"Entity '{entityName}' not found.");

        var info = _registry.GetByName(entityName)
            ?? throw new KeyNotFoundException($"Entity '{entityName}' not found in registry.");

        var db = _dbContextProvider.GetDbContext(info.ClrType);

        List<List<string>> allRows;
        if (fileType.Contains("csv", StringComparison.OrdinalIgnoreCase))
        {
            (_, allRows) = await ParseCsvAsync(fileStream, ct);
        }
        else
        {
            (_, allRows) = ParseExcel(fileStream);
        }

        var errors = new List<ImportError>();
        var successCount = 0;

        // Field name → metadata lookup
        var fieldMap = metadata.Fields.ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

        for (var rowIdx = 0; rowIdx < allRows.Count; rowIdx++)
        {
            var row = allRows[rowIdx];
            var rowNumber = rowIdx + 2; // Excel 1-indexed + header row

            try
            {
                var entityDict = new Dictionary<string, object?>();

                foreach (var (colIdx, fieldName) in columnMapping)
                {
                    if (colIdx >= row.Count) continue;

                    var cellValue = row[colIdx];
                    if (!fieldMap.TryGetValue(fieldName, out var fieldMeta)) continue;

                    var converted = ConvertValue(cellValue, fieldMeta.Type);
                    entityDict[fieldName] = converted;
                }

                // Validation
                var validationErrors = ValidateRow(entityDict, columnMapping.Values.ToList(), fieldMap, rowNumber);
                if (validationErrors.Count > 0)
                {
                    errors.AddRange(validationErrors);
                    continue;
                }

                // Entity oluştur
                var json = JsonSerializer.Serialize(entityDict, JsonOptions);
                var entity = JsonSerializer.Deserialize(json, info.ClrType, JsonOptions);
                if (entity is null)
                {
                    errors.Add(new ImportError { RowNumber = rowNumber, Field = "", Message = "Satır parse edilemedi" });
                    continue;
                }

                // Id ata
                var idProp = info.ClrType.GetProperty("Id");
                if (idProp is not null)
                {
                    var currentId = idProp.GetValue(entity);
                    if (currentId is Guid guid && guid == Guid.Empty)
                    {
                        idProp.SetValue(entity, Guid.NewGuid());
                    }
                }

                // CreatedAt set et
                SetProperty(entity, "CreatedAt", DateTime.UtcNow);

                db.Add(entity);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Field = "",
                    Message = ex.Message
                });
            }
        }

        // Toplu kayıt
        if (successCount > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("[DynamicImport] Import {Entity}: {Success} success, {Error} errors",
            entityName, successCount, errors.Count);

        return new ImportResult
        {
            SuccessCount = successCount,
            ErrorCount = errors.Count,
            TotalCount = allRows.Count,
            Errors = errors
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  PARSE HELPERS
    // ═══════════════════════════════════════════════════════════

    private static (List<string> headers, List<List<string>> rows) ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow < 1 || lastCol < 1)
            return ([], []);

        // Headers (row 1)
        var headers = new List<string>();
        for (var col = 1; col <= lastCol; col++)
        {
            headers.Add(worksheet.Cell(1, col).GetString().Trim());
        }

        // 2. satır hint row ise atla (italic check)
        var dataStartRow = 2;
        if (lastRow >= 2)
        {
            var firstDataCell = worksheet.Cell(2, 1);
            if (firstDataCell.Style.Font.Italic)
                dataStartRow = 3;
        }

        // Data rows
        var rows = new List<List<string>>();
        for (var row = dataStartRow; row <= lastRow; row++)
        {
            var rowData = new List<string>();
            var isEmpty = true;
            for (var col = 1; col <= lastCol; col++)
            {
                var val = worksheet.Cell(row, col).GetString().Trim();
                rowData.Add(val);
                if (!string.IsNullOrEmpty(val)) isEmpty = false;
            }
            if (!isEmpty) rows.Add(rowData);
        }

        return (headers, rows);
    }

    private static async Task<(List<string> headers, List<List<string>> rows)> ParseCsvAsync(
        Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.GetCultureInfo("tr-TR"))
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        });

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? [];

        var rows = new List<List<string>>();
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            var row = new List<string>();
            for (var i = 0; i < headers.Count; i++)
            {
                row.Add(csv.GetField(i)?.Trim() ?? "");
            }
            rows.Add(row);
        }

        return (headers, rows);
    }

    // ═══════════════════════════════════════════════════════════
    //  CONVERSION + VALIDATION
    // ═══════════════════════════════════════════════════════════

    private static object? ConvertValue(string cellValue, string fieldType)
    {
        if (string.IsNullOrWhiteSpace(cellValue)) return null;

        return fieldType switch
        {
            "boolean" => cellValue.Equals("Evet", StringComparison.OrdinalIgnoreCase)
                         || cellValue.Equals("true", StringComparison.OrdinalIgnoreCase)
                         || cellValue == "1",

            "number" => int.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var intVal)
                ? intVal
                : (object?)null,

            "decimal" or "money" => decimal.TryParse(
                cellValue.Replace("₺", "").Replace(",", ".").Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var decVal)
                ? decVal
                : null,

            "date" or "datetime" => DateTime.TryParse(cellValue, CultureInfo.GetCultureInfo("tr-TR"),
                DateTimeStyles.None, out var dtVal)
                ? dtVal
                : DateTime.TryParse(cellValue, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dtVal2)
                    ? dtVal2
                    : null,

            "lookup" => Guid.TryParse(cellValue, out var guidVal) ? guidVal : null,

            _ => cellValue // string, text, enum, etc.
        };
    }

    private static List<ImportError> ValidateRow(
        Dictionary<string, object?> entityDict,
        List<string> mappedFields,
        Dictionary<string, Models.FieldMetadataDto> fieldMap,
        int rowNumber)
    {
        var errors = new List<ImportError>();

        foreach (var (fieldName, meta) in fieldMap)
        {
            if (!meta.Required) continue;
            if (meta.ReadOnly || !string.IsNullOrEmpty(meta.Computed)) continue;

            // Bu alan mapping'de varsa kontrol et
            if (!mappedFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                continue;

            entityDict.TryGetValue(fieldName, out var val);

            if (val is null || (val is string str && string.IsNullOrWhiteSpace(str)))
            {
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Field = meta.Label,
                    Message = $"'{meta.Label}' alanı zorunludur"
                });
            }
        }

        return errors;
    }

    private static void SetProperty(object entity, string propertyName, object value)
    {
        var prop = entity.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        if (prop is not null && prop.CanWrite)
            prop.SetValue(entity, value);
    }
}
