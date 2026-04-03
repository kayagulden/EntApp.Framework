namespace EntApp.Shared.Contracts.Common;

/// <summary>
/// Tenant bilgisi DTO.
/// Modüller arası tenant referanslarında kullanılır.
/// </summary>
public sealed record TenantInfoDto
{
    /// <summary>Tenant kimliği.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Tenant adı.</summary>
    public required string TenantName { get; init; }

    /// <summary>Tenant slug (subdomain veya route prefix).</summary>
    public string? Slug { get; init; }

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; init; } = true;
}
