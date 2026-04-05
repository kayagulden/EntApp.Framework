using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>Talep yorumu — dahili/harici ayrımı ile.</summary>
public sealed class TicketComment : AuditableEntity<TicketCommentId>, ITenantEntity
{
    public TicketId TicketId { get; private set; }
    public string Content { get; private set; } = string.Empty;

    /// <summary>true = dahili yorum (müşteri/talep sahibi göremez).</summary>
    public bool IsInternal { get; private set; }

    public Guid AuthorUserId { get; private set; }
    public Guid TenantId { get; set; }

    // Navigation
    public Ticket Ticket { get; private set; } = null!;

    private TicketComment() { }

    public static TicketComment Create(TicketId ticketId, string content, Guid authorUserId, bool isInternal = false)
    {
        return new TicketComment
        {
            Id = EntityId.New<TicketCommentId>(),
            TicketId = ticketId,
            Content = content,
            AuthorUserId = authorUserId,
            IsInternal = isInternal
        };
    }
}
