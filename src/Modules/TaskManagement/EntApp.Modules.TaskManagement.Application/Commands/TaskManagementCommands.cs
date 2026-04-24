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
public sealed record CreateWorkItemCommand(Guid? ProjectId, string Title, string Type = "Task",
    string Priority = "Medium", string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, Guid? ParentTaskId = null, DateTime? DueDate = null,
    decimal EstimatedHours = 0, string? Tags = null) : IRequest<CreateWorkItemResult>;
public sealed record CreateWorkItemResult(Guid Id, string WorkItemNumber);

/// <summary>Dış kaynaktan (Ticket vb.) iş kalemi oluşturur. Tip ve parent desteği ile.</summary>
public sealed record CreateWorkItemFromSourceCommand(
    string SourceModule, string SourceType, Guid SourceId,
    string Title, string? Description = null,
    Guid? AssigneeUserId = null, Guid? ReporterUserId = null,
    string Priority = "Medium", DateTime? DueDate = null,
    Guid? ProjectId = null, string WorkItemType = "Task",
    Guid? ParentWorkItemId = null) : IRequest<CreateWorkItemResult>;

public sealed record MoveWorkItemCommand(Guid TaskId, string Status, int? SortOrder = null) : IRequest<MoveWorkItemResult>;
public sealed record MoveWorkItemResult(Guid Id, string Status, int SortOrder);

public sealed record AssignWorkItemCommand(Guid TaskId, Guid? UserId) : IRequest<Guid?>;
public sealed record CreateCommentCommand(Guid TaskId, Guid AuthorUserId, string Content) : IRequest<Guid>;
public sealed record CreateTimeEntryCommand(Guid TaskId, Guid UserId, decimal Hours,
    DateTime WorkDate, string? Description = null) : IRequest<Guid>;

public sealed record UpdateWorkItemCommand(Guid TaskId, string? Title = null, string? Description = null,
    string? Priority = null, string? Type = null, DateTime? DueDate = null,
    decimal? EstimatedHours = null, string? Tags = null, Guid? AssigneeUserId = null,
    int? StoryPoints = null, string? AcceptanceCriteria = null,
    Guid? SprintId = null,
    int? BusinessValue = null, int? TimeCriticality = null, int? RiskReduction = null) : IRequest<Guid>;

// ── Sprint ──────────────────────────────────────────────────
public sealed record CreateSprintCommand(Guid ProjectId, string Name,
    DateTime StartDate, DateTime EndDate,
    string? Goal = null, int? CapacityPoints = null) : IRequest<Guid>;

public sealed record UpdateSprintCommand(Guid SprintId, string? Name = null,
    string? Goal = null, DateTime? StartDate = null, DateTime? EndDate = null,
    int? CapacityPoints = null) : IRequest<Guid>;

public sealed record StartSprintCommand(Guid SprintId) : IRequest<Guid>;
public sealed record CompleteSprintCommand(Guid SprintId) : IRequest<Guid>;

/// <summary>Work item'ı bir sprint'e atar veya sprint'ten çıkarır.</summary>
public sealed record AssignToSprintCommand(Guid TaskId, Guid? SprintId) : IRequest<Guid>;

// ── BoardColumn ─────────────────────────────────────────────
public sealed record CreateBoardColumnCommand(Guid ProjectId, string Name,
    int Order, string MappedStatus, int? WipLimit = null) : IRequest<Guid>;

public sealed record UpdateBoardColumnCommand(Guid ColumnId, string? Name = null,
    int? Order = null, int? WipLimit = null, string? MappedStatus = null) : IRequest<Guid>;

public sealed record DeleteBoardColumnCommand(Guid ColumnId) : IRequest;

public sealed record ReorderBoardColumnsCommand(Guid ProjectId,
    List<Guid> ColumnIds) : IRequest;

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

// ── Milestone ───────────────────────────────────────────────
public sealed record CreateMilestoneCommand(Guid ProjectId, string Name,
    DateTime DueDate, string? Description = null, int SortOrder = 0) : IRequest<Guid>;

public sealed record UpdateMilestoneCommand(Guid MilestoneId, string? Name = null,
    string? Description = null, DateTime? DueDate = null,
    int? SortOrder = null, string? Status = null) : IRequest<Guid>;

public sealed record DeleteMilestoneCommand(Guid MilestoneId) : IRequest;

// ── Ticket → Project Promotion ──────────────────────────────
/// <summary>
/// Ticket'ı bir projenin backlog'una aktarır.
/// Parent WorkItem oluşturur ve mevcut ticket task'larını altına taşır.
/// </summary>
public sealed record PromoteTicketToProjectCommand(
    Guid TicketId, Guid ProjectId, string Title,
    string WorkItemType = "Feature", string Priority = "Medium",
    string? Description = null) : IRequest<PromoteTicketResult>;

public sealed record PromoteTicketResult(Guid ParentWorkItemId, int MovedTaskCount);

// ── Project Template ────────────────────────────────────────
public sealed record CreateProjectTemplateCommand(string Name, string? Description = null,
    string? Icon = null, string? Methodology = null, string? Category = null,
    string? EstimationMode = null, int SortOrder = 0,
    string? BoardColumnsJson = null, string? MilestonesJson = null,
    string? WorkItemsJson = null) : IRequest<Guid>;

public sealed record UpdateProjectTemplateCommand(Guid TemplateId, string? Name = null,
    string? Description = null, string? Icon = null,
    string? Methodology = null, string? Category = null,
    string? EstimationMode = null, int? SortOrder = null, bool? IsActive = null,
    string? BoardColumnsJson = null, string? MilestonesJson = null,
    string? WorkItemsJson = null) : IRequest<Guid>;

public sealed record DeleteProjectTemplateCommand(Guid TemplateId) : IRequest;

/// <summary>Şablondan yeni proje oluşturur — board kolonları, milestone'lar ve iş kalemleri otomatik oluşturulur.</summary>
public sealed record CreateProjectFromTemplateCommand(Guid TemplateId,
    string Key, string Name, string? Description = null,
    DateTime? StartDate = null, DateTime? TargetEndDate = null,
    Guid? ManagerUserId = null, Guid? OwnerUserId = null,
    Guid? PortfolioId = null) : IRequest<Guid>;

// ── Requirement ────────────────────────────────────────────

public sealed record CreateRequirementCommand(Guid ProjectId, string Title,
    string Type = "Functional", string Priority = "Must",
    string? Description = null, string? AcceptanceCriteria = null,
    Guid? ParentRequirementId = null,
    Guid? SourceTicketId = null, string? SourceTicketNumber = null,
    string? ExternalDesignUrl = null) : IRequest<Guid>;

public sealed record UpdateRequirementCommand(Guid RequirementId,
    string? Title = null, string? Description = null,
    string? Type = null, string? Priority = null, string? Status = null,
    string? AcceptanceCriteria = null, string? ExternalDesignUrl = null) : IRequest<Guid>;

public sealed record DeleteRequirementCommand(Guid RequirementId) : IRequest;

// ── Test Scenario ──────────────────────────────────────────

public sealed record CreateTestScenarioCommand(Guid ProjectId, string Title,
    string Type = "Functional", string Priority = "Medium",
    string? Description = null, string? Preconditions = null,
    Guid? RequirementId = null,
    int? EstimatedDurationMinutes = null, string? Tags = null) : IRequest<Guid>;

public sealed record UpdateTestScenarioCommand(Guid TestScenarioId,
    string? Title = null, string? Description = null,
    string? Type = null, string? Priority = null, string? Status = null,
    string? Preconditions = null, Guid? RequirementId = null,
    int? EstimatedDurationMinutes = null, string? Tags = null) : IRequest<Guid>;

public sealed record DeleteTestScenarioCommand(Guid TestScenarioId) : IRequest;

// ── Test Step ──────────────────────────────────────────────

public sealed record UpdateTestStepsCommand(Guid TestScenarioId,
    List<TestStepDto> Steps) : IRequest;

public sealed record TestStepDto(string Action, string ExpectedResult,
    int StepNumber, string? TestData = null, string? Notes = null, Guid? Id = null);

// ── Test Plan ──────────────────────────────────────────────

public sealed record CreateTestPlanCommand(Guid ProjectId, string Title,
    string? Description = null,
    Guid? SprintId = null, Guid? MilestoneId = null,
    string? StartDate = null, string? EndDate = null,
    string? AssignedTesterId = null) : IRequest<Guid>;

public sealed record UpdateTestPlanCommand(Guid TestPlanId,
    string? Title = null, string? Description = null,
    string? Status = null,
    string? StartDate = null, string? EndDate = null,
    string? AssignedTesterId = null) : IRequest<Guid>;

public sealed record DeleteTestPlanCommand(Guid TestPlanId) : IRequest;

// ── Test Plan Scenario ─────────────────────────────────────

public sealed record AddScenarioToTestPlanCommand(Guid TestPlanId, Guid TestScenarioId,
    string? AssignedTesterId = null) : IRequest<Guid>;

public sealed record RemoveScenarioFromTestPlanCommand(Guid TestPlanId, Guid TestScenarioId) : IRequest;

// ── Test Execution ─────────────────────────────────────────

public sealed record RecordTestExecutionCommand(Guid TestPlanId, Guid TestScenarioId,
    string Result, string? ExecutedBy = null, string? Notes = null, string? Environment = null,
    int? DurationMinutes = null,
    List<TestStepResultDto>? StepResults = null) : IRequest<Guid>;

public sealed record TestStepResultDto(Guid TestStepId, string Result,
    string? ActualResult = null, string? Notes = null);

// ── Release ────────────────────────────────────────────────

public sealed record CreateReleaseCommand(Guid ProjectId, string Version, string Title,
    string Type = "Minor", string? Description = null,
    Guid? SprintId = null, Guid? MilestoneId = null,
    string? PlannedDate = null, string? CodeFreezeDate = null,
    string? ReleaseManagerId = null, string? TargetEnvironment = null,
    string? Tags = null) : IRequest<Guid>;

public sealed record UpdateReleaseCommand(Guid ReleaseId,
    string? Version = null, string? Title = null, string? Description = null,
    string? Type = null,
    string? PlannedDate = null, string? ActualDate = null, string? CodeFreezeDate = null,
    string? ReleaseManagerId = null, string? TargetEnvironment = null,
    string? Tags = null) : IRequest<Guid>;

public sealed record DeleteReleaseCommand(Guid ReleaseId) : IRequest;

public sealed record UpdateReleaseStatusCommand(Guid ReleaseId, string Status) : IRequest<Guid>;

// ── Release Item ───────────────────────────────────────────

public sealed record AddReleaseItemCommand(Guid ReleaseId, Guid WorkItemId,
    string? Notes = null) : IRequest<Guid>;

public sealed record RemoveReleaseItemCommand(Guid ReleaseId, Guid WorkItemId) : IRequest;

public sealed record AddReleaseItemsFromSprintCommand(Guid ReleaseId, Guid SprintId) : IRequest<int>;

// ── Go/No-Go ──────────────────────────────────────────────

/// <summary>Varsayılan maddelerle checklist oluşturur.</summary>
public sealed record CreateGoNoGoChecklistCommand(Guid ReleaseId) : IRequest<Guid>;

public sealed record AddGoNoGoItemCommand(Guid ReleaseId,
    string Category, string Title, string? Description = null,
    bool IsRequired = true) : IRequest<Guid>;

public sealed record UpdateGoNoGoItemCommand(Guid ReleaseId, Guid ItemId,
    string Status, string? ReviewedBy = null, string? Notes = null) : IRequest<Guid>;

public sealed record DecideGoNoGoCommand(Guid ReleaseId,
    string Status, string? DecisionBy = null, string? DecisionNotes = null) : IRequest<Guid>;

// ── Release Note ──────────────────────────────────────────

/// <summary>Release'deki WorkItem'lardan otomatik release note üretir.</summary>
public sealed record GenerateReleaseNoteCommand(Guid ReleaseId) : IRequest<Guid>;

public sealed record UpdateReleaseNoteCommand(Guid ReleaseId, string Content) : IRequest<Guid>;

