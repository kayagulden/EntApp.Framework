using EntApp.Modules.KnowledgeBase.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.KnowledgeBase.Domain.Entities;

/// <summary>Wiki alanı — proje bazlı veya global.</summary>
public sealed class WikiSpace : AggregateRoot<WikiSpaceId>, ITenantEntity
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>URL-friendly benzersiz tanımlayıcı.</summary>
    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>null ise global wiki, değilse proje bazlı.</summary>
    public Guid? ProjectId { get; private set; }

    /// <summary>Alan ikonu (emoji).</summary>
    public string? IconEmoji { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<WikiPage> Pages { get; private set; } = [];

    private WikiSpace() { }

    public static WikiSpace Create(string name, string slug,
        string? description = null, Guid? projectId = null, string? iconEmoji = null)
    {
        return new WikiSpace
        {
            Id = EntityId.New<WikiSpaceId>(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Description = description,
            ProjectId = projectId,
            IconEmoji = iconEmoji
        };
    }

    public void Update(string? name = null, string? description = null, string? iconEmoji = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (iconEmoji is not null) IconEmoji = iconEmoji;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
