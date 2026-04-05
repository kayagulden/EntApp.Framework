using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>
/// Kuyruk üyeliği — hangi kullanıcı hangi kuyruğun üyesi.
/// Üyeler kuyruğa düşen talepleri görüp claim edebilir.
/// </summary>
public sealed class QueueMembership : BaseEntity<QueueMembershipId>
{
    public ServiceQueueId QueueId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Üye rolü: Member, Lead, Dispatcher.</summary>
    public string Role { get; private set; } = "Member";

    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
    public bool IsActive { get; private set; } = true;

    // Navigation
    public ServiceQueue Queue { get; private set; } = null!;

    private QueueMembership() { }

    public static QueueMembership Create(ServiceQueueId queueId, Guid userId, string role = "Member")
    {
        return new QueueMembership
        {
            Id = EntityId.New<QueueMembershipId>(),
            QueueId = queueId,
            UserId = userId,
            Role = role
        };
    }

    public void UpdateRole(string role) => Role = role;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
