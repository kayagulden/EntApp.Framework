using Microsoft.AspNetCore.Authorization;

namespace EntApp.Shared.Infrastructure.Auth;

/// <summary>
/// Controller/endpoint üzerine konan permission-based authorization attribute.
/// Keycloak permission claim'i checker.
/// </summary>
/// <example>
/// <code>
/// [HasPermission("products.create")]
/// public async Task&lt;IActionResult&gt; CreateProduct(...)
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Permission policy prefix.</summary>
    internal const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permission)
        : base($"{PolicyPrefix}{permission}")
    {
    }
}
