using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Görev yorumu.</summary>
public sealed class CommentBase : AuditableEntity<Guid>, ITenantEntity
{
    public Guid TaskId { get; private set; }
    public Guid AuthorUserId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public Guid TenantId { get; set; }

    // Navigation
    public TaskItemBase Task { get; private set; } = null!;

    private CommentBase() { }

    public static CommentBase Create(Guid taskId, Guid authorUserId, string content)
    {
        return new CommentBase
        {
            Id = Guid.NewGuid(), TaskId = taskId,
            AuthorUserId = authorUserId, Content = content
        };
    }
}
