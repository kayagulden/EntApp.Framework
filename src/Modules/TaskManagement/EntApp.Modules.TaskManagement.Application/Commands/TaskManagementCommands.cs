using MediatR;

namespace EntApp.Modules.TaskManagement.Application.Commands;

// ── Portfolio ────────────────────────────────────────────────
public sealed record CreatePortfolioCommand(string Name, string Code,
    string? Description = null, Guid? OwnerUserId = null) : IRequest<Guid>;

public sealed record UpdatePortfolioCommand(Guid PortfolioId, string? Name = null,
    string? Code = null, string? Description = null,
    Guid? OwnerUserId = null, string? Status = null) : IRequest<Guid>;

// ── Application ─────────────────────────────────────────────
public sealed record CreateApplicationCommand(string Name, string Code,
    string? Description = null, string ApplicationType = "InHouse",
    string Criticality = "Medium", Guid? OwnerUserId = null,
    Guid? TechLeadUserId = null, string? TechnologyStack = null,
    string? RepositoryUrl = null, string? DocumentationUrl = null,
    string? CurrentVersion = null) : IRequest<Guid>;

public sealed record UpdateApplicationCommand(Guid ApplicationId, string? Name = null,
    string? Description = null, string? ApplicationType = null,
    string? Status = null, string? Criticality = null,
    Guid? OwnerUserId = null, Guid? TechLeadUserId = null,
    string? TechnologyStack = null, string? RepositoryUrl = null,
    string? DocumentationUrl = null, string? CurrentVersion = null) : IRequest<Guid>;

// ── Project ─────────────────────────────────────────────────
public sealed record CreateProjectCommand(string Key, string Name, string? Description = null,
    DateTime? StartDate = null, DateTime? EndDate = null, DateTime? TargetEndDate = null,
    Guid? ManagerUserId = null, Guid? OwnerUserId = null,
    Guid? PortfolioId = null, string Methodology = "Kanban",
    string Category = "General") : IRequest<Guid>;

public sealed record UpdateProjectCommand(Guid ProjectId, string? Name = null,
    string? Description = null, DateTime? StartDate = null, DateTime? EndDate = null,
    DateTime? TargetEndDate = null, Guid? ManagerUserId = null, Guid? OwnerUserId = null,
    Guid? PortfolioId = null, string? Status = null, string? Methodology = null,
    string? Category = null) : IRequest<Guid>;

// ── Task ────────────────────────────────────────────────────
public sealed record CreateTaskCommand(Guid? ProjectId, string Title, string Type = "Task",
    string Priority = "Medium", string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, Guid? ParentTaskId = null, DateTime? DueDate = null,
    decimal EstimatedHours = 0, string? Tags = null) : IRequest<CreateTaskResult>;
public sealed record CreateTaskResult(Guid Id, string TaskNumber);

/// <summary>Dış kaynaktan (Ticket vb.) görev oluşturur.</summary>
public sealed record CreateTaskFromSourceCommand(
    string SourceModule, string SourceType, Guid SourceId,
    string Title, string? Description = null,
    Guid? AssigneeUserId = null, Guid? ReporterUserId = null,
    string Priority = "Medium", DateTime? DueDate = null,
    Guid? ProjectId = null) : IRequest<CreateTaskResult>;

public sealed record MoveTaskCommand(Guid TaskId, string Status, int? SortOrder = null) : IRequest<MoveTaskResult>;
public sealed record MoveTaskResult(Guid Id, string Status, int SortOrder);

public sealed record AssignTaskCommand(Guid TaskId, Guid? UserId) : IRequest<Guid?>;
public sealed record CreateCommentCommand(Guid TaskId, Guid AuthorUserId, string Content) : IRequest<Guid>;
public sealed record CreateTimeEntryCommand(Guid TaskId, Guid UserId, decimal Hours,
    DateTime WorkDate, string? Description = null) : IRequest<Guid>;

public sealed record UpdateTaskCommand(Guid TaskId, string? Title = null, string? Description = null,
    string? Priority = null, string? Type = null, DateTime? DueDate = null,
    decimal? EstimatedHours = null, string? Tags = null, Guid? AssigneeUserId = null) : IRequest<Guid>;

// ── ProjectDeliverable ─────────────────────────────────────
public sealed record AddProjectDeliverableCommand(
    Guid ProjectId, Guid ConfigurationItemId,
    string Role = "Primary", string? Notes = null) : IRequest<Guid>;

public sealed record RemoveProjectDeliverableCommand(
    Guid ProjectId, Guid ConfigurationItemId) : IRequest;

// ── Server ──────────────────────────────────────────────────
public sealed record CreateServerCommand(string Name, string Code,
    string? Description = null, string ServerType = "Virtual",
    string Environment = "Production", string Criticality = "Medium",
    Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? OperatingSystem = null, string? IpAddress = null,
    string? Hostname = null, int? CpuCores = null, int? RamGB = null,
    int? DiskGB = null, string? DataCenter = null) : IRequest<Guid>;

public sealed record UpdateServerCommand(Guid ServerId, string? Name = null,
    string? Description = null, string? ServerType = null,
    string? Environment = null, string? Status = null, string? Criticality = null,
    Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? OperatingSystem = null, string? IpAddress = null,
    string? Hostname = null, int? CpuCores = null, int? RamGB = null,
    int? DiskGB = null, string? DataCenter = null) : IRequest<Guid>;

// ── Database ────────────────────────────────────────────────
public sealed record CreateDatabaseCommand(string Name, string Code,
    string? Description = null, string DatabaseEngine = "PostgreSQL",
    string Criticality = "Medium",
    Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? Version = null, int? Port = null, decimal? SizeGB = null,
    string? ConnectionString = null, string? BackupSchedule = null) : IRequest<Guid>;

public sealed record UpdateDatabaseCommand(Guid DatabaseId, string? Name = null,
    string? Description = null, string? DatabaseEngine = null,
    string? Status = null, string? Criticality = null,
    Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? Version = null, int? Port = null, decimal? SizeGB = null,
    string? ConnectionString = null, string? BackupSchedule = null) : IRequest<Guid>;

// ── Licence ─────────────────────────────────────────────────
public sealed record CreateLicenceCommand(string Name, string Code,
    string? Description = null, string LicenceType = "Subscription",
    string Criticality = "Medium", Guid? OwnerUserId = null,
    string? Vendor = null, string? ProductName = null, string? LicenceKey = null,
    int? MaxUsers = null, int? CurrentUsers = null,
    DateTime? ExpirationDate = null, DateTime? PurchaseDate = null,
    decimal? AnnualCost = null, string? Currency = null) : IRequest<Guid>;

public sealed record UpdateLicenceCommand(Guid LicenceId, string? Name = null,
    string? Description = null, string? LicenceType = null,
    string? Status = null, string? Criticality = null, Guid? OwnerUserId = null,
    string? Vendor = null, string? ProductName = null, string? LicenceKey = null,
    int? MaxUsers = null, int? CurrentUsers = null,
    DateTime? ExpirationDate = null, DateTime? PurchaseDate = null,
    decimal? AnnualCost = null, string? Currency = null) : IRequest<Guid>;

// ── CIRelationship ──────────────────────────────────────────
public sealed record AddCIRelationshipCommand(
    Guid SourceCIId, Guid TargetCIId,
    string RelationType, string? Notes = null) : IRequest<Guid>;

public sealed record RemoveCIRelationshipCommand(Guid RelationshipId) : IRequest;
