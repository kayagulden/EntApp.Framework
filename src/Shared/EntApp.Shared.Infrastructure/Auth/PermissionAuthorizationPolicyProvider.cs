using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EntApp.Shared.Infrastructure.Auth;

/// <summary>
/// "Permission:xxx" policy'lerini otomatik oluşturan policy provider.
/// HasPermissionAttribute ile birlikte çalışır.
/// </summary>
public sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        // Standart policy'leri kontrol et
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null)
        {
            return policy;
        }

        // "Permission:xxx" formatında mı?
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];

            return new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
        }

        return null;
    }
}
