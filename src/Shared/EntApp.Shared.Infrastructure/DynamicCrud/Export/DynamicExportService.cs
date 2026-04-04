using System.Globalization;
using System.Reflection;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using EntApp.Shared.Kernel.Domain.Attributes;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.DynamicCrud.Export;

/// <summary>
/// Entity verilerini Excel veya CSV formatında dışa aktarır.
/// Metadata'dan kolon başlıklarını ve formatlama kurallarını alır.
/// </summary>
public interface IDynamicExportService
{
    Task<byte[]> ExportToExcelAsync(string entityName, CancellationToken ct = default);
    Task<byte[]> ExportToCsvAsync(string entityName, CancellationToken ct = default);
}

public sealed class DynamicExportService : IDynamicExportService
{
    private readonly IDynamicEntityRegistry _registry;
    private readonly IDynamicDbContextProvider _dbContextProvider;
    private readonly IMetadataService _metadataService;
    private readonly ILogger<DynamicExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DynamicExportService(
        IDynamicEntityRegistry registry,
        IDynamicDbContextProvider dbContextProvider,
        IMetadataService metadataService,
        ILogger<DynamicExportService> logger)
    {
        _registry = registry;
        _dbContextProvider = dbContextProvider;
        _metadataService = metadataService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  EXCEL EXPORT
    // ═══════════════════════════════════════════════════════════

    public async Task<byte[]> ExportToExcelAsync(string entityName, CancellationToken ct = default)
    {
        var (metadata, items) = await GetExportDataAsync(entityName, ct);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(metadata.Title.Length > 31
            ? metadata.Title[..31]
            : metadata.Title);

        var fields = metadata.Fields.ToList();

        // Header row
        for (var i = 0; i < fields.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = fields[i].Label;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1E293B);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        for (var row = 0; row < items.Count; row++)
        {
            for (var col = 0; col < fields.Count; col++)
            {
                var cell = worksheet.Cell(row + 2, col + 1);
                var field = fields[col];
                var propName = field.Name;

                // camelCase → PascalCase çevir
                var pascalName = char.ToUpperInvariant(propName[0]) + propName[1..];
                items[row].TryGetValue(propName, out var value);
                if (value is null)
                    items[row].TryGetValue(pascalName, out value);

                SetCellValue(cell, value, field.Type);
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents(1, Math.Min(items.Count + 1, 1000));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation("[DynamicExport] Excel export {Entity}: {Count} rows",
            entityName, items.Count);

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    //  CSV EXPORT
    // ═══════════════════════════════════════════════════════════

    public async Task<byte[]> ExportToCsvAsync(string entityName, CancellationToken ct = default)
    {
        var (metadata, items) = await GetExportDataAsync(entityName, ct);

        var fields = metadata.Fields.ToList();

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.GetCultureInfo("tr-TR"))
        {
            Delimiter = ";",
            HasHeaderRecord = true,
        });

        // Header
        foreach (var field in fields)
        {
            csv.WriteField(field.Label);
        }
        await csv.NextRecordAsync();

        // Data
        foreach (var item in items)
        {
            foreach (var field in fields)
            {
                var propName = field.Name;
                var pascalName = char.ToUpperInvariant(propName[0]) + propName[1..];
                item.TryGetValue(propName, out var value);
                if (value is null)
                    item.TryGetValue(pascalName, out value);

                csv.WriteField(FormatCsvValue(value, field.Type));
            }
            await csv.NextRecordAsync();
        }

        await csv.FlushAsync();
        await writer.FlushAsync(ct);

        _logger.LogInformation("[DynamicExport] CSV export {Entity}: {Count} rows",
            entityName, items.Count);

        return stream.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE
    // ═══════════════════════════════════════════════════════════

    private Task<(Models.EntityMetadataDto metadata, List<Dictionary<string, object?>> items)>
        GetExportDataAsync(string entityName, CancellationToken ct)
    {
        var metadata = _metadataService.GetMetadata(entityName)
            ?? throw new KeyNotFoundException($"Entity '{entityName}' not found.");

        var info = _registry.GetByName(entityName)
            ?? throw new KeyNotFoundException($"Entity '{entityName}' not found in registry.");

        var db = _dbContextProvider.GetDbContext(info.ClrType);

        // DbContext.Set<T>() via reflection
        var setMethod = typeof(Microsoft.EntityFrameworkCore.DbContext).GetMethods()
            .First(m => m.Name == "Set" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
        var genericSet = setMethod.MakeGenericMethod(info.ClrType);
        var dbSet = genericSet.Invoke(db, null) as IQueryable<object>
            ?? throw new InvalidOperationException($"Cannot get DbSet for {entityName}");

        // Tüm kayıtları çek (export — paging yok)
        var entities = dbSet.ToList();

        // Entity → Dictionary dönüşümü
        var items = entities.Select(entity =>
        {
            var json = JsonSerializer.Serialize(entity, info.ClrType, JsonOptions);
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions) ?? new();
        }).ToList();

        return Task.FromResult((metadata, items));
    }

    private static void SetCellValue(IXLCell cell, object? value, string fieldType)
    {
        if (value is null || value is JsonElement { ValueKind: JsonValueKind.Null })
        {
            cell.Value = Blank.Value;
            return;
        }

        // JsonElement çözümleme
        if (value is JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.String:
                    var str = je.GetString() ?? "";
                    if (fieldType is "date" or "datetime" && DateTime.TryParse(str, out var dt))
                    {
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = fieldType == "date"
                            ? "dd.MM.yyyy"
                            : "dd.MM.yyyy HH:mm";
                    }
                    else
                    {
                        cell.Value = str;
                    }
                    return;
                case JsonValueKind.Number:
                    var num = je.GetDouble();
                    cell.Value = num;
                    if (fieldType == "money")
                        cell.Style.NumberFormat.Format = "#,##0.00 ₺";
                    else if (fieldType == "decimal")
                        cell.Style.NumberFormat.Format = "#,##0.00";
                    return;
                case JsonValueKind.True:
                    cell.Value = "Evet";
                    return;
                case JsonValueKind.False:
                    cell.Value = "Hayır";
                    return;
                default:
                    cell.Value = je.ToString();
                    return;
            }
        }

        cell.Value = value.ToString();
    }

    private static string FormatCsvValue(object? value, string fieldType)
    {
        if (value is null) return "";

        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString() ?? "",
                JsonValueKind.Number => je.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonValueKind.True => "Evet",
                JsonValueKind.False => "Hayır",
                JsonValueKind.Null => "",
                _ => je.ToString()
            };
        }

        return value.ToString() ?? "";
    }
}
