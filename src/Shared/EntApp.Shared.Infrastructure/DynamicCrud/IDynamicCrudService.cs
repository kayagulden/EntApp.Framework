using System.Text.Json;
using EntApp.Shared.Contracts.Common;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// Entity adına göre generic CRUD operasyonları gerçekleştirir.
/// EF Core DbContext üzerinde reflection ile çalışır.
/// </summary>
public interface IDynamicCrudService
{
    /// <summary>Sayfalanmış liste.</summary>
    Task<PagedResult<JsonElement>> GetPagedAsync(string entityName, PagedRequest request, CancellationToken ct = default);

    /// <summary>Tekil kayıt.</summary>
    Task<JsonElement?> GetByIdAsync(string entityName, Guid id, CancellationToken ct = default);

    /// <summary>Yeni kayıt oluştur.</summary>
    Task<Guid> CreateAsync(string entityName, JsonElement body, CancellationToken ct = default);

    /// <summary>Kayıt güncelle.</summary>
    Task UpdateAsync(string entityName, Guid id, JsonElement body, CancellationToken ct = default);

    /// <summary>Soft delete.</summary>
    Task DeleteAsync(string entityName, Guid id, CancellationToken ct = default);

    /// <summary>Lookup arama.</summary>
    Task<IReadOnlyList<LookupDto>> LookupAsync(string entityName, string? search = null, int take = 20, CancellationToken ct = default);
}
