using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Audit.Domain.Entities;

/// <summary>
/// Giriş/çıkış kayıtları.
/// </summary>
public class LoginRecord : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public LoginResult Result { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime Timestamp { get; private set; }
    public Guid? TenantId { get; private set; }

    private LoginRecord() { }

    public static LoginRecord Create(
        Guid userId, string userName, LoginResult result,
        string? ipAddress, string? userAgent,
        string? failureReason = null, Guid? tenantId = null)
    {
        return new LoginRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Result = result,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            Timestamp = DateTime.UtcNow,
            TenantId = tenantId
        };
    }
}

public enum LoginResult
{
    Success,
    Failed,
    LockedOut,
    RequiresTwoFactor
}
