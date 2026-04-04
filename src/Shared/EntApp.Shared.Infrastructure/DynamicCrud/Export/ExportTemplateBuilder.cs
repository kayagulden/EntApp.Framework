using ClosedXML.Excel;

namespace EntApp.Shared.Infrastructure.DynamicCrud.Export;

/// <summary>
/// Entity metadata'sından boş Excel import şablonu üretir.
/// Kullanıcı bu şablonu indirip doldurduktan sonra import eder.
/// </summary>
public sealed class ExportTemplateBuilder
{
    private readonly IMetadataService _metadataService;

    public ExportTemplateBuilder(IMetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    /// <summary>
    /// Entity metadata'sından boş Excel şablonu oluşturur.
    /// Sadece editable field'ların label'ları header olarak yazılır.
    /// </summary>
    public byte[] BuildTemplate(string entityName)
    {
        var metadata = _metadataService.GetMetadata(entityName)
            ?? throw new KeyNotFoundException($"Entity '{entityName}' not found.");

        // Sadece editable alanlar (readOnly ve computed hariç)
        var editableFields = metadata.Fields
            .Where(f => !f.ReadOnly && string.IsNullOrEmpty(f.Computed))
            .ToList();

        using var workbook = new XLWorkbook();
        var sheetName = metadata.Title.Length > 31
            ? metadata.Title[..31]
            : metadata.Title;
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Header row
        for (var i = 0; i < editableFields.Count; i++)
        {
            var field = editableFields[i];
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = field.Label;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x6366F1);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Validation hints row (2. satır — açıklama)
        for (var i = 0; i < editableFields.Count; i++)
        {
            var field = editableFields[i];
            var cell = worksheet.Cell(2, i + 1);
            cell.Value = BuildHint(field);
            cell.Style.Font.Italic = true;
            cell.Style.Font.FontColor = XLColor.Gray;
            cell.Style.Font.FontSize = 9;
        }

        // Auto-fit
        worksheet.Columns().AdjustToContents();

        // Minimum kolon genişliği
        foreach (var col in worksheet.Columns())
        {
            if (col.Width < 15) col.Width = 15;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string BuildHint(Models.FieldMetadataDto field)
    {
        var parts = new List<string> { field.Type };

        if (field.Required) parts.Add("zorunlu");
        if (field.MaxLength > 0) parts.Add($"maks {field.MaxLength} karakter");
        if (field.Options is { Count: > 0 }) parts.Add($"seçenekler: {string.Join(", ", field.Options)}");

        return $"({string.Join(" | ", parts)})";
    }
}
