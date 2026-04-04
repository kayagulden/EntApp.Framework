namespace EntApp.Shared.Infrastructure.DynamicCrud.Import;

/// <summary>
/// Excel/CSV kolon başlıklarını entity metadata field'larıyla otomatik eşleştirir.
/// Exact match → contains match → similarity fallback sırasıyla çalışır.
/// </summary>
public static class ColumnMapper
{
    /// <summary>
    /// Dosya kolon başlıkları ile entity field label/name'lerini eşleştirir.
    /// Sonuç: kolon index → field name dictionary.
    /// </summary>
    public static Dictionary<int, string> AutoMap(
        IReadOnlyList<string> fileHeaders,
        IReadOnlyList<Models.FieldMetadataDto> fields)
    {
        var mapping = new Dictionary<int, string>();
        var usedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Sadece yazılabilir alanlar
        var editableFields = fields
            .Where(f => !f.ReadOnly && string.IsNullOrEmpty(f.Computed))
            .ToList();

        for (var i = 0; i < fileHeaders.Count; i++)
        {
            var header = Normalize(fileHeaders[i]);
            if (string.IsNullOrWhiteSpace(header)) continue;

            // 1) Exact match (label veya name)
            var match = editableFields.FirstOrDefault(f =>
                !usedFields.Contains(f.Name) &&
                (Normalize(f.Label) == header || Normalize(f.Name) == header));

            // 2) Contains match
            if (match is null)
            {
                match = editableFields.FirstOrDefault(f =>
                    !usedFields.Contains(f.Name) &&
                    (Normalize(f.Label).Contains(header) || header.Contains(Normalize(f.Label))));
            }

            // 3) Starts-with match
            if (match is null)
            {
                match = editableFields.FirstOrDefault(f =>
                    !usedFields.Contains(f.Name) &&
                    (Normalize(f.Label).StartsWith(header) || header.StartsWith(Normalize(f.Label))));
            }

            if (match is not null)
            {
                mapping[i] = match.Name;
                usedFields.Add(match.Name);
            }
        }

        return mapping;
    }

    private static string Normalize(string input)
    {
        return input.Trim()
            .ToLowerInvariant()
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "");
    }
}
