using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Portfolyo — projeleri stratejik olarak gruplandıran üst kategori.</summary>
public sealed class PortfolioBase : AuditableEntity<PortfolioId>, ITenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PortfolioStatus Status { get; private set; } = PortfolioStatus.Active;

    /// <summary>Portfolyo sahibi (stratejik sorumlu).</summary>
    public Guid? OwnerUserId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<ProjectBase> Projects { get; private set; } = [];

    private PortfolioBase() { }

    public static PortfolioBase Create(string name, string code, string? description = null,
        Guid? ownerUserId = null)
    {
        return new PortfolioBase
        {
            Id = EntityId.New<PortfolioId>(),
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            OwnerUserId = ownerUserId
        };
    }

    public void Update(string? name = null, string? code = null, string? description = null,
        Guid? ownerUserId = null, PortfolioStatus? status = null)
    {
        if (name is not null) Name = name;
        if (code is not null) Code = code.ToUpperInvariant();
        if (description is not null) Description = description;
        if (ownerUserId.HasValue) OwnerUserId = ownerUserId.Value;
        if (status.HasValue) Status = status.Value;
    }

    public void Archive() => Status = PortfolioStatus.Archived;
}
