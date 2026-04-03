using System.Security.Claims;
using EntApp.Shared.Contracts.Identity;
using Microsoft.AspNetCore.Http;

namespace EntApp.Shared.Infrastructure.Auth;

/// <summary>
/// HttpContext üzerinden JWT claim'lerini okuyarak ICurrentUser implementasyonu sağlar.
/// Keycloak claim yapısıyla uyumlu.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Keycloak claim tipleri.</summary>
    private const string SubClaim = "sub";
    private const string PreferredUsernameClaim = "preferred_username";
    private const string EmailClaim = "email";
    private const string RealmRoleClaim = "realm_role";
    private const string PermissionClaim = "permission";

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirstValue(SubClaim)
                      ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string UserName =>
        User?.FindFirstValue(PreferredUsernameClaim)
        ?? User?.FindFirstValue(ClaimTypes.Name)
        ?? "anonymous";

    public string? Email =>
        User?.FindFirstValue(EmailClaim)
        ?? User?.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User is null) return Array.Empty<string>();
            return User.FindAll(RealmRoleClaim)
                .Select(c => c.Value)
                .Concat(User.FindAll(ClaimTypes.Role).Select(c => c.Value))
                .Distinct()
                .ToList()
                .AsReadOnly();
        }
    }

    public IReadOnlyList<string> Permissions
    {
        get
        {
            if (User is null) return Array.Empty<string>();
            return User.FindAll(PermissionClaim)
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly();
        }
    }

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
