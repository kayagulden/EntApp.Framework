namespace EntApp.Shared.Contracts.Common;

/// <summary>
/// Kullanıcı bilgisi DTO.
/// Modüller arası kullanıcı referanslarında kullanılır.
/// </summary>
public sealed record UserInfoDto
{
    /// <summary>Kullanıcı kimliği.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Kullanıcı adı.</summary>
    public required string UserName { get; init; }

    /// <summary>Tam ad (Ad + Soyad).</summary>
    public string? FullName { get; init; }

    /// <summary>E-posta.</summary>
    public string? Email { get; init; }

    /// <summary>Profil resmi URL.</summary>
    public string? AvatarUrl { get; init; }
}
