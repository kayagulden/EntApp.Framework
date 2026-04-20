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
    int TaskCount, int TaskSequence,
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
public sealed record ListTasksQuery(Guid? ProjectId, string? Status, string? Assignee, string? Priority,
    int Page = 1, int PageSize = 20, Guid? ReporterUserId = null, string? AssigneeUserIds = null,
    string? Type = null, string? SourceFilter = null) : IRequest<PagedResult<object>>;
public sealed record GetTaskQuery(Guid Id) : IRequest<TaskDetailDto?>;
public sealed record GetKanbanBoardQuery(Guid ProjectId) : IRequest<object>;
public sealed record ListCommentsQuery(Guid TaskId) : IRequest<List<object>>;
public sealed record ListTimeEntriesQuery(Guid? TaskId, Guid? UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;

/// <summary>Belirli bir kaynağa (Ticket vb.) bağlı görevleri listeler.</summary>
public sealed record ListTasksBySourceQuery(
    string SourceModule, string SourceType, Guid SourceId
) : IRequest<List<TaskBySourceDto>>;

public sealed record TaskBySourceDto(
    Guid Id, string TaskNumber, string Title, string Status,
    string Priority, string Type, Guid? AssigneeUserId,
    DateTime? DueDate, decimal EstimatedHours, DateTime CreatedAt);

public sealed record TaskDetailDto(
    Guid Id, string TaskNumber, string Title, string? Description,
    string Status, string Priority, string Type,
    Guid? AssigneeUserId, Guid? ReporterUserId,
    Guid? ParentTaskId, DateTime? DueDate, decimal EstimatedHours,
    int SortOrder, string? Tags,
    string? SourceModule, string? SourceType, Guid? SourceId,
    Guid? ProjectId, string? ProjectKey, string? ProjectName,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<TaskBySourceDto> SubTasks);
