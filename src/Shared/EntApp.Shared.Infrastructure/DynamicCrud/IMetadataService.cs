using EntApp.Shared.Infrastructure.DynamicCrud.Models;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// Entity CLR tipinden metadata JSON schema üretir.
/// Attribute + convention-based + DB override bilgilerini birleştirir (3-tier fallback).
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Entity adına göre metadata döner (sync — sadece attribute/convention, DB override yok).
    /// Geriye uyumluluk için korunur.
    /// </summary>
    EntityMetadataDto? GetMetadata(string entityName);

    /// <summary>
    /// Entity adına göre metadata döner (async — DB override merge'li, 3-tier fallback).
    /// DB → Convention → Attribute sırasıyla override uygulanır.
    /// </summary>
    Task<EntityMetadataDto?> GetMetadataAsync(string entityName, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Sidebar menu yapısını döner (detail entity'ler hariç).</summary>
    IReadOnlyList<MenuGroupDto> GetMenu();
}
