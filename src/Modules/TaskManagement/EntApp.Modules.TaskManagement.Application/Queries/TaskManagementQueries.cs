using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.TaskManagement.Application.Queries;

// ── Portfolio ────────────────────────────────────────────────
public sealed record ListPortfoliosQuery(string? Status = null) : IRequest<List<PortfolioListDto>>;
public sealed record GetPortfolioQuery(Guid Id) : IRequest<PortfolioDetailDto?>;

public sealed record PortfolioListDto(
    Guid Id, string Name, string Code, string? Description,
    string Status, Guid? OwnerUserId, int ProjectCount,
    DateTime CreatedAt);

public sealed record PortfolioDetailDto(
    Guid Id, string Name, string Code, string? Description,
    string Status, Guid? OwnerUserId,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<ProjectListDto> Projects);

// ── Application ─────────────────────────────────────────────
public sealed record ListApplicationsQuery(string? Status = null, string? ApplicationType = null) : IRequest<List<ApplicationListDto>>;
public sealed record GetApplicationQuery(Guid Id) : IRequest<ApplicationDetailDto?>;

public sealed record ApplicationListDto(
    Guid Id, string Name, string Code, string? Description,
    string ApplicationType, string Status, string Criticality,
    Guid? OwnerUserId, Guid? TechLeadUserId,
    string? TechnologyStack, string? CurrentVersion,
    DateTime CreatedAt);

public sealed record ApplicationDetailDto(
    Guid Id, string Name, string Code, string? Description,
    string ApplicationType, string Status, string Criticality,
    Guid? OwnerUserId, Guid? TechLeadUserId,
    string? TechnologyStack, string? RepositoryUrl,
    string? DocumentationUrl, string? CurrentVersion,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<CIProjectDto>? Projects = null);

// ── Project ─────────────────────────────────────────────────
public sealed record ListProjectsQuery(string? Status = null, Guid? PortfolioId = null) : IRequest<List<ProjectListDto>>;
public sealed record GetProjectQuery(Guid Id) : IRequest<ProjectDetailDto?>;

public sealed record ProjectListDto(
    Guid Id, string Key, string Name, string? Description,
    string Status, string Methodology, string Category,
    DateTime? StartDate, DateTime? EndDate, DateTime? TargetEndDate,
    Guid? ManagerUserId, Guid? OwnerUserId,
    Guid? PortfolioId, string? PortfolioName,
    int TaskCount, DateTime CreatedAt);

public sealed record ProjectDetailDto(
    Guid Id, string Key, string Name, string? Description,
    string Status, string Methodology, string Category,
    DateTime? StartDate, DateTime? EndDate, DateTime? TargetEndDate,
    Guid? ManagerUserId, Guid? OwnerUserId,
    Guid? PortfolioId, string? PortfolioName, string? PortfolioCode,
    int TaskCount, int WorkItemSequence,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<ProjectDeliverableDto>? Deliverables = null);

// ── ProjectDeliverable ──────────────────────────────────────
public sealed record ProjectDeliverableDto(
    Guid Id, Guid ConfigurationItemId, string CIName, string CICode,
    string CIType, string Role, string? Notes);

public sealed record CIProjectDto(
    Guid ProjectId, string ProjectKey, string ProjectName,
    string ProjectStatus, string Role, string? Notes);

public sealed record ListProjectDeliverablesQuery(Guid ProjectId)
    : IRequest<List<ProjectDeliverableDto>>;

public sealed record ListCIProjectsQuery(Guid ConfigurationItemId)
    : IRequest<List<CIProjectDto>>;

// ── Task ────────────────────────────────────────────────────
public sealed record ListWorkItemsQuery(Guid? ProjectId, string? Status, string? Assignee, string? Priority,
    int Page = 1, int PageSize = 20, Guid? ReporterUserId = null, string? AssigneeUserIds = null,
    string? Type = null, string? SourceFilter = null) : IRequest<PagedResult<object>>;
public sealed record GetWorkItemQuery(Guid Id) : IRequest<WorkItemDetailDto?>;
public sealed record GetKanbanBoardQuery(
    Guid ProjectId,
    Guid? SprintId = null,
    Guid? AssigneeUserId = null,
    bool IncludeCompleted = false) : IRequest<object>;
public sealed record ListCommentsQuery(Guid TaskId) : IRequest<List<object>>;
public sealed record ListTimeEntriesQuery(Guid? TaskId, Guid? UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;

/// <summary>Belirli bir kaynağa (Ticket vb.) bağlı görevleri listeler.</summary>
public sealed record ListWorkItemsBySourceQuery(
    string SourceModule, string SourceType, Guid SourceId
) : IRequest<List<WorkItemBySourceDto>>;

public sealed record WorkItemBySourceDto(
    Guid Id, string WorkItemNumber, string Title, string Status,
    string Priority, string Type, Guid? AssigneeUserId,
    DateTime? DueDate, decimal EstimatedHours, DateTime CreatedAt,
    int? StoryPoints = null, int HierarchyLevel = 0,
    Guid? ProjectId = null, decimal? WsjfScore = null);

public sealed record WorkItemDetailDto(
    Guid Id, string WorkItemNumber, string Title, string? Description,
    string Status, string Priority, string Type,
    Guid? AssigneeUserId, Guid? ReporterUserId,
    Guid? ParentTaskId, DateTime? DueDate, decimal EstimatedHours,
    int SortOrder, string? Tags,
    string? SourceModule, string? SourceType, Guid? SourceId,
    Guid? ProjectId, string? ProjectKey, string? ProjectName,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<WorkItemBySourceDto> SubTasks,
    int? StoryPoints = null, string? AcceptanceCriteria = null,
    Guid? SprintId = null, string? SprintName = null,
    int HierarchyLevel = 0,
    int? BusinessValue = null, int? TimeCriticality = null,
    int? RiskReduction = null, decimal? WsjfScore = null);

// ── Sprint ──────────────────────────────────────────────────
public sealed record ListSprintsQuery(Guid ProjectId, string? Status = null) : IRequest<List<SprintListDto>>;
public sealed record GetSprintQuery(Guid Id) : IRequest<SprintDetailDto?>;

public sealed record SprintListDto(
    Guid Id, string Name, string? Goal, string Status,
    DateTime StartDate, DateTime EndDate, int? CapacityPoints,
    Guid ProjectId, string ProjectKey,
    int WorkItemCount, int? TotalStoryPoints,
    DateTime CreatedAt);

public sealed record SprintDetailDto(
    Guid Id, string Name, string? Goal, string Status,
    DateTime StartDate, DateTime EndDate, int? CapacityPoints,
    Guid ProjectId, string ProjectKey, string ProjectName,
    int WorkItemCount, int? TotalStoryPoints,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<WorkItemBySourceDto> WorkItems);

// ── BoardColumn ─────────────────────────────────────────────
public sealed record ListBoardColumnsQuery(Guid ProjectId) : IRequest<List<BoardColumnDto>>;

public sealed record BoardColumnDto(Guid Id, string Name, int Order,
    string MappedStatus, int? WipLimit);

// ── Velocity & Burndown ─────────────────────────────────────
public sealed record GetVelocityQuery(Guid ProjectId) : IRequest<List<VelocityDto>>;
public sealed record VelocityDto(Guid SprintId, string SprintName,
    int PlannedPoints, int CompletedPoints,
    DateTime StartDate, DateTime EndDate);

public sealed record GetBurndownQuery(Guid SprintId) : IRequest<BurndownChartDto>;
public sealed record BurndownChartDto(int PlannedPoints, DateTime StartDate, DateTime EndDate,
    List<BurndownPointDto> DataPoints);
public sealed record BurndownPointDto(DateTime Date, int RemainingPoints, int CompletedPoints,
    int TotalItems, int CompletedItems);

// ── Metrics Summary ─────────────────────────────────────────
public sealed record GetMetricsSummaryQuery(Guid ProjectId) : IRequest<MetricsSummaryDto>;
public sealed record MetricsSummaryDto(
    int TotalStoryPoints, int CompletedStoryPoints,
    int TotalWorkItems, int ActiveWorkItems,
    decimal AverageVelocity,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByPriority,
    decimal? AverageLeadTimeDays,
    decimal? AverageCycleTimeDays);

// ── Backlog ─────────────────────────────────────────────────
/// <summary>Proje backlog'unu flat veya tree olarak getirir.</summary>
public sealed record GetBacklogQuery(Guid ProjectId,
    string View = "flat",      // flat | tree
    string? Type = null,        // virgülle ayrılmış WorkItemType listesi
    string? Sprint = null,      // "current" | sprint guid
    Guid? Assignee = null,
    string? Status = null       // virgülle ayrılmış TaskStatus listesi
) : IRequest<object>;

// ── Server ──────────────────────────────────────────────────
public sealed record ListServersQuery(string? Status = null, string? ServerType = null,
    string? Environment = null) : IRequest<List<ServerListDto>>;
public sealed record GetServerQuery(Guid Id) : IRequest<ServerDetailDto?>;

public sealed record ServerListDto(
    Guid Id, string Name, string Code, string? Description,
    string ServerType, string Environment, string Status, string Criticality,
    string? OperatingSystem, string? IpAddress, string? Hostname,
    int? CpuCores, int? RamGB, int? DiskGB, string? DataCenter,
    Guid? OwnerUserId, Guid? AdminUserId,
    DateTime CreatedAt);

public sealed record ServerDetailDto(
    Guid Id, string Name, string Code, string? Description,
    string ServerType, string Environment, string Status, string Criticality,
    string? OperatingSystem, string? IpAddress, string? Hostname,
    int? CpuCores, int? RamGB, int? DiskGB, string? DataCenter,
    Guid? OwnerUserId, Guid? AdminUserId,
    DateTime CreatedAt, DateTime? UpdatedAt);

// ── Database ────────────────────────────────────────────────
public sealed record ListDatabasesQuery(string? Status = null,
    string? DatabaseEngine = null) : IRequest<List<DatabaseListDto>>;
public sealed record GetDatabaseQuery(Guid Id) : IRequest<DatabaseDetailDto?>;

public sealed record DatabaseListDto(
    Guid Id, string Name, string Code, string? Description,
    string DatabaseEngine, string Status, string Criticality,
    string? Version, int? Port, decimal? SizeGB,
    string? BackupSchedule,
    Guid? OwnerUserId, Guid? AdminUserId,
    DateTime CreatedAt);

public sealed record DatabaseDetailDto(
    Guid Id, string Name, string Code, string? Description,
    string DatabaseEngine, string Status, string Criticality,
    string? Version, int? Port, decimal? SizeGB,
    string? ConnectionString, string? BackupSchedule,
    Guid? OwnerUserId, Guid? AdminUserId,
    DateTime CreatedAt, DateTime? UpdatedAt);

// ── Licence ─────────────────────────────────────────────────
public sealed record ListLicencesQuery(string? Status = null,
    string? LicenceType = null) : IRequest<List<LicenceListDto>>;
public sealed record GetLicenceQuery(Guid Id) : IRequest<LicenceDetailDto?>;

public sealed record LicenceListDto(
    Guid Id, string Name, string Code, string? Description,
    string LicenceType, string Status, string Criticality,
    string? Vendor, string? ProductName,
    int? MaxUsers, int? CurrentUsers,
    DateTime? ExpirationDate, decimal? AnnualCost, string? Currency,
    Guid? OwnerUserId,
    DateTime CreatedAt);

public sealed record LicenceDetailDto(
    Guid Id, string Name, string Code, string? Description,
    string LicenceType, string Status, string Criticality,
    string? Vendor, string? ProductName, string? LicenceKey,
    int? MaxUsers, int? CurrentUsers,
    DateTime? ExpirationDate, DateTime? PurchaseDate,
    decimal? AnnualCost, string? Currency,
    Guid? OwnerUserId,
    DateTime CreatedAt, DateTime? UpdatedAt);

// ── CIRelationship ──────────────────────────────────────────
public sealed record ListCIRelationshipsQuery(Guid CIId) : IRequest<List<CIRelationshipDto>>;

public sealed record CIRelationshipDto(
    Guid Id, Guid SourceCIId, string SourceName, string SourceCode, string SourceType,
    Guid TargetCIId, string TargetName, string TargetCode, string TargetType,
    string RelationType, string? Notes,
    string Direction); // "outgoing" or "incoming" relative to the queried CI

// ── Milestone ───────────────────────────────────────────────
public sealed record ListMilestonesQuery(Guid ProjectId, string? Status = null) : IRequest<List<MilestoneListDto>>;
public sealed record GetMilestoneQuery(Guid MilestoneId) : IRequest<MilestoneDetailDto?>;

public sealed record MilestoneListDto(
    Guid Id, string Name, string? Description, string Status,
    DateTime DueDate, DateTime? CompletedDate,
    int SortOrder, int WorkItemCount, int SprintCount,
    DateTime CreatedAt);

public sealed record MilestoneDetailDto(
    Guid Id, string Name, string? Description, string Status,
    DateTime DueDate, DateTime? CompletedDate,
    int SortOrder, int WorkItemCount, int SprintCount,
    DateTime CreatedAt, DateTime? UpdatedAt);

// ── Project Template ────────────────────────────────────────
public sealed record ListProjectTemplatesQuery(bool IncludeInactive = false) : IRequest<List<ProjectTemplateListDto>>;
public sealed record GetProjectTemplateQuery(Guid Id) : IRequest<ProjectTemplateDetailDto?>;

public sealed record ProjectTemplateListDto(
    Guid Id, string Name, string? Description, string? Icon,
    string Methodology, string Category, string EstimationMode,
    bool IsBuiltIn, bool IsActive, int SortOrder,
    DateTime CreatedAt);

public sealed record ProjectTemplateDetailDto(
    Guid Id, string Name, string? Description, string? Icon,
    string Methodology, string Category, string EstimationMode,
    bool IsBuiltIn, bool IsActive, int SortOrder,
    string BoardColumnsJson, string? MilestonesJson, string? WorkItemsJson,
    DateTime CreatedAt, DateTime? UpdatedAt);

// ── Requirement ────────────────────────────────────────────

public sealed record ListRequirementsQuery(Guid ProjectId,
    Guid? ParentId = null, string? Type = null, string? Status = null)
    : IRequest<List<RequirementListDto>>;

public sealed record GetRequirementQuery(Guid RequirementId)
    : IRequest<RequirementDetailDto?>;

public sealed record RequirementListDto(
    Guid Id, string Key, string Title,
    string Type, string Priority, string Status,
    Guid? ParentRequirementId,
    int ChildCount, int WorkItemCount,
    int SortOrder, DateTime CreatedAt);

public sealed record RequirementDetailDto(
    Guid Id, string Key, string Title,
    string Type, string Priority, string Status,
    string? Description, string? AcceptanceCriteria,
    Guid? ParentRequirementId,
    Guid? SourceTicketId, string? SourceTicketNumber,
    string? ExternalDesignUrl,
    int SortOrder, DateTime CreatedAt,
    List<RequirementListDto>? Children = null,
    List<RequirementWorkItemDto>? WorkItems = null);

public sealed record RequirementWorkItemDto(
    Guid Id, string WorkItemNumber, string Title,
    string Status, string Type, string Priority,
    Guid? AssigneeUserId);

// ── Test Scenario ──────────────────────────────────────────

public sealed record ListTestScenariosQuery(Guid ProjectId,
    string? Type = null, string? Status = null, string? Priority = null,
    Guid? RequirementId = null) : IRequest<List<TestScenarioListDto>>;

public sealed record GetTestScenarioQuery(Guid TestScenarioId)
    : IRequest<TestScenarioDetailDto?>;

public sealed record TestScenarioListDto(
    Guid Id, string Key, string Title,
    string Type, string Priority, string Status,
    Guid? RequirementId, string? RequirementKey,
    int StepCount, int ExecutionCount,
    string? Tags, int? EstimatedDurationMinutes,
    int SortOrder, DateTime CreatedAt);

public sealed record TestScenarioDetailDto(
    Guid Id, string Key, string Title,
    string Type, string Priority, string Status,
    string? Description, string? Preconditions,
    Guid? RequirementId, string? RequirementKey,
    int? EstimatedDurationMinutes, string? Tags,
    int SortOrder, DateTime CreatedAt,
    List<TestStepListDto>? Steps = null);

public sealed record TestStepListDto(
    Guid Id, int StepNumber, string Action, string ExpectedResult,
    string? TestData, string? Notes);

// ── Test Plan ──────────────────────────────────────────────

public sealed record ListTestPlansQuery(Guid ProjectId,
    string? Status = null) : IRequest<List<TestPlanListDto>>;

public sealed record GetTestPlanQuery(Guid TestPlanId)
    : IRequest<TestPlanDetailDto?>;

public sealed record TestPlanListDto(
    Guid Id, string Key, string Title, string Status,
    Guid? SprintId, Guid? MilestoneId,
    string? StartDate, string? EndDate,
    string? AssignedTesterId,
    int ScenarioCount, int PassCount, int FailCount, int NotRunCount,
    DateTime CreatedAt);

public sealed record TestPlanDetailDto(
    Guid Id, string Key, string Title, string Status,
    string? Description,
    Guid? SprintId, Guid? MilestoneId,
    string? StartDate, string? EndDate,
    string? AssignedTesterId,
    DateTime CreatedAt,
    List<TestPlanScenarioDto>? Scenarios = null);

public sealed record TestPlanScenarioDto(
    Guid TestPlanScenarioId,
    Guid TestScenarioId, string TestScenarioKey, string TestScenarioTitle,
    string TestScenarioType, string TestScenarioPriority,
    string? AssignedTesterId,
    string? LastResult, DateTime? LastExecutedAt,
    int SortOrder);

// ── Test Execution ─────────────────────────────────────────

public sealed record ListTestExecutionsQuery(Guid TestPlanId,
    Guid? TestScenarioId = null) : IRequest<List<TestExecutionListDto>>;

public sealed record ListScenarioExecutionsQuery(Guid TestScenarioId)
    : IRequest<List<TestExecutionListDto>>;

public sealed record TestExecutionListDto(
    Guid Id, string Result,
    string ExecutedBy, DateTime ExecutedAt,
    int? DurationMinutes, string? Notes, string? Environment,
    Guid? LinkedBugId,
    string? TestScenarioKey, string? TestPlanKey,
    List<TestStepResultListDto>? StepResults = null);

public sealed record TestStepResultListDto(
    Guid TestStepId, int StepNumber, string Action,
    string Result, string? ActualResult, string? Notes);

// ── Release ────────────────────────────────────────────────

public sealed record ListReleasesQuery(Guid ProjectId,
    string? Status = null, string? Type = null) : IRequest<List<ReleaseListDto>>;

public sealed record GetReleaseQuery(Guid ReleaseId) : IRequest<ReleaseDetailDto?>;

public sealed record ReleaseListDto(
    Guid Id, string Key, string Version, string Title,
    string Status, string Type,
    string? PlannedDate, string? ActualDate,
    string? ReleaseManagerId, string? TargetEnvironment,
    Guid? SprintId, Guid? MilestoneId,
    int ItemCount, string? Tags,
    int SortOrder, DateTime CreatedAt);

public sealed record ReleaseDetailDto(
    Guid Id, string Key, string Version, string Title,
    string Status, string Type,
    string? Description,
    string? PlannedDate, string? ActualDate, string? CodeFreezeDate,
    string? ReleaseManagerId, string? TargetEnvironment,
    Guid? SprintId, Guid? MilestoneId,
    string? Tags, int SortOrder,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<ReleaseItemDto>? Items = null,
    GoNoGoChecklistSummaryDto? GoNoGoChecklist = null,
    ReleaseNoteDto? ReleaseNote = null);

// ── Release Item ───────────────────────────────────────────

public sealed record ListReleaseItemsQuery(Guid ReleaseId) : IRequest<List<ReleaseItemDto>>;

public sealed record ReleaseItemDto(
    Guid Id, Guid WorkItemId, string WorkItemNumber, string WorkItemTitle,
    string WorkItemType, string WorkItemStatus,
    DateTime IncludedAt, string IncludedBy, string? Notes,
    int SortOrder);

// ── Go/No-Go ──────────────────────────────────────────────

public sealed record GetGoNoGoChecklistQuery(Guid ReleaseId) : IRequest<GoNoGoChecklistDetailDto?>;

public sealed record GoNoGoChecklistSummaryDto(
    Guid Id, string Status, int TotalItems,
    int ApprovedCount, int RejectedCount, int PendingCount,
    string? DecisionBy, DateTime? DecisionAt);

public sealed record GoNoGoChecklistDetailDto(
    Guid Id, Guid ReleaseId, string Status,
    string? DecisionBy, DateTime? DecisionAt, string? DecisionNotes,
    DateTime CreatedAt,
    List<GoNoGoItemDto> Items);

public sealed record GoNoGoItemDto(
    Guid Id, string Category, string Title, string? Description,
    string Status, string? ReviewedBy, DateTime? ReviewedAt,
    string? Notes, int SortOrder, bool IsRequired);

// ── Release Note ──────────────────────────────────────────

public sealed record GetReleaseNoteQuery(Guid ReleaseId) : IRequest<ReleaseNoteDto?>;

public sealed record ReleaseNoteDto(
    Guid Id, Guid ReleaseId, string Content,
    DateTime GeneratedAt, bool IsManuallyEdited, DateTime? PublishedAt);

