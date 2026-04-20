using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Proje teslim edilebiliri — Proje ↔ CI (Application, Server vb.) many-to-many ara tablosu.
/// Hangi CI'ların bu projenin çıktısı olduğunu ve rollerini tanımlar.
/// </summary>
public sealed class ProjectDeliverable : AuditableEntity<ProjectDeliverableId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }
    public ConfigurationItemId ConfigurationItemId { get; private set; }

    /// <summary>CI'ın projedeki rolü (Primary, Secondary, Supporting).</summary>
    public DeliverableRole Role { get; private set; } = DeliverableRole.Primary;

    /// <summary>Opsiyonel açıklama notu.</summary>
    public string? Notes { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProjectBase? Project { get; private set; }
    public ConfigurationItemBase? ConfigurationItem { get; private set; }

    private ProjectDeliverable() { }

    public static ProjectDeliverable Create(ProjectId projectId, ConfigurationItemId ciId,
        DeliverableRole role = DeliverableRole.Primary, string? notes = null)
    {
        return new ProjectDeliverable
        {
            Id = EntityId.New<ProjectDeliverableId>(),
            ProjectId = projectId,
            ConfigurationItemId = ciId,
            Role = role,
            Notes = notes
        };
    }

    public void UpdateRole(DeliverableRole role) => Role = role;
    public void UpdateNotes(string? notes) => Notes = notes;
}
