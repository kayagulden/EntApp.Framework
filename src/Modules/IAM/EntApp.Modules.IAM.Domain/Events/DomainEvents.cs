using EntApp.Shared.Kernel.Domain.Events;

namespace EntApp.Modules.IAM.Domain.Events;

/// <summary>Kullanıcı oluşturulduğunda.</summary>
public sealed record UserCreatedEvent(Guid UserId, string UserName, string Email) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>Kullanıcıya rol atandığında.</summary>
public sealed record RoleAssignedEvent(Guid UserId, Guid RoleId, string RoleName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>Kullanıcı deaktif edildiğinde.</summary>
public sealed record UserDeactivatedEvent(Guid UserId, string UserName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
