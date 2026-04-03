namespace EntApp.Shared.Contracts.Identity;

/// <summary>
/// Mevcut kullanıcı bilgisine erişim kontratı.
/// HttpContext'ten çözümlenir (Shared.Infrastructure'da implement edilir).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Kullanıcı kimliği (Keycloak sub claim).</summary>
    Guid UserId { get; }

    /// <summary>Kullanıcı adı.</summary>
    string UserName { get; }

    /// <summary>E-posta adresi.</summary>
    string? Email { get; }

    /// <summary>Kullanıcının rolleri.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Kullanıcının yetkileri (permission claim'leri).</summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>Kullanıcı kimliği doğrulanmış mı?</summary>
    bool IsAuthenticated { get; }

    /// <summary>Belirtilen role sahip mi?</summary>
    bool IsInRole(string role);

    /// <summary>Belirtilen yetkiye sahip mi?</summary>
    bool HasPermission(string permission);
}
