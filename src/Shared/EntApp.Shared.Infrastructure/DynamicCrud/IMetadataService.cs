using EntApp.Shared.Infrastructure.DynamicCrud.Models;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// Entity CLR tipinden metadata JSON schema üretir.
/// Attribute + convention-based bilgileri birleştirir.
/// </summary>
public interface IMetadataService
{
    /// <summary>Entity adına göre metadata döner.</summary>
    EntityMetadataDto? GetMetadata(string entityName);

    /// <summary>Sidebar menu yapısını döner (detail entity'ler hariç).</summary>
    IReadOnlyList<MenuGroupDto> GetMenu();
}
