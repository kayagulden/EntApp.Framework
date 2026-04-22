using EntApp.Modules.StateFlow.Domain.Enums;
using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Entities;

namespace EntApp.Modules.StateFlow.Domain.Entities;

/// <summary>
/// Durum Akışı Tanımı — AggregateRoot.
/// Bir entity tipi (Ticket, WorkItem vb.) için state machine konfigürasyonunu tutar.
/// Versiyonlanabilir: Draft → Published → Archived yaşam döngüsü.
/// </summary>
public sealed class StateFlowDefinition : AggregateRoot<StateFlowDefinitionId>, ITenantEntity
{
    /// <summary>Hangi entity tipinin akışı: "Ticket", "WorkItem", "ChangeRequest".</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Benzersiz anahtar: "ticket-default-flow".</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>İnsan okunabilir ad: "Destek Talebi Akışı".</summary>
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>Versiyon numarası (1, 2, 3...).</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Akış durumu: Draft, Published, Archived.</summary>
    public FlowStatus Status { get; private set; } = FlowStatus.Draft;

    public DateTime? PublishedAt { get; private set; }

    /// <summary>Global şablon mu? Global tanımlar tenant'lar tarafından kopyalanabilir.</summary>
    public bool IsGlobalTemplate { get; private set; }

    /// <summary>Kopyalandığı global şablonun ID'si (izlenebilirlik için).</summary>
    public Guid? SourceTemplateId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<StateDefinition> States { get; private set; } = [];
    public ICollection<TransitionDefinition> Transitions { get; private set; } = [];

    private StateFlowDefinition() { }

    public static StateFlowDefinition Create(
        string entityType, string key, string name,
        string? description = null, bool isGlobalTemplate = false,
        Guid? sourceTemplateId = null)
    {
        return new StateFlowDefinition
        {
            Id = EntityId.New<StateFlowDefinitionId>(),
            EntityType = entityType,
            Key = key,
            Name = name,
            Description = description,
            Version = 1,
            Status = FlowStatus.Draft,
            IsGlobalTemplate = isGlobalTemplate,
            SourceTemplateId = sourceTemplateId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Draft akışı yayınlar. Önceki Published akışın Archived olması handler'da yapılır.</summary>
    public void Publish()
    {
        if (Status != FlowStatus.Draft)
            throw new InvalidOperationException("Only Draft flows can be published.");

        Status = FlowStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    /// <summary>Yayınlanmış akışı arşivler (yeni versiyon yayınlandığında).</summary>
    public void Archive()
    {
        if (Status != FlowStatus.Published)
            throw new InvalidOperationException("Only Published flows can be archived.");

        Status = FlowStatus.Archived;
    }

    /// <summary>Akışın temel bilgilerini günceller (sadece Draft'ta).</summary>
    public void Update(string name, string? description)
    {
        if (Status != FlowStatus.Draft)
            throw new InvalidOperationException("Only Draft flows can be edited.");

        Name = name;
        Description = description;
    }

    /// <summary>Bu akıştan yeni bir Draft versiyon oluşturur (state + transition kopyası handler'da yapılır).</summary>
    public static StateFlowDefinition CreateNewVersion(StateFlowDefinition source)
    {
        return new StateFlowDefinition
        {
            Id = EntityId.New<StateFlowDefinitionId>(),
            EntityType = source.EntityType,
            Key = source.Key,
            Name = source.Name,
            Description = source.Description,
            Version = source.Version + 1,
            Status = FlowStatus.Draft,
            IsGlobalTemplate = source.IsGlobalTemplate,
            SourceTemplateId = source.SourceTemplateId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Global şablondan tenant'a özel kopya oluşturur.</summary>
    public static StateFlowDefinition CloneFromTemplate(StateFlowDefinition template, string? customName = null)
    {
        return new StateFlowDefinition
        {
            Id = EntityId.New<StateFlowDefinitionId>(),
            EntityType = template.EntityType,
            Key = $"{template.Key}-custom",
            Name = customName ?? template.Name,
            Description = template.Description,
            Version = 1,
            Status = FlowStatus.Draft,
            IsGlobalTemplate = false,
            SourceTemplateId = template.Id.Value,
            CreatedAt = DateTime.UtcNow
        };
    }
}
