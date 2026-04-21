using EntApp.Modules.TaskManagement.Application.Commands;
using EntApp.Modules.TaskManagement.Application.IntegrationEvents;
using EntApp.Modules.TaskManagement.Application.Queries;
using EntApp.Modules.TaskManagement.Domain.Entities;
using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Modules.TaskManagement.Infrastructure.Persistence;
using EntApp.Modules.TaskManagement.Infrastructure.Services;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Contracts.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.TaskStatus;

namespace EntApp.Modules.TaskManagement.Infrastructure.Handlers;

// ── Portfolio Queries ────────────────────────────────────────
public sealed class ListPortfoliosQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListPortfoliosQuery, List<PortfolioListDto>>
{
    public async Task<List<PortfolioListDto>> Handle(ListPortfoliosQuery request, CancellationToken ct)
    {
        var query = db.Portfolios.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PortfolioStatus>(request.Status, out var s))
            query = query.Where(p => p.Status == s);
        return await query.OrderBy(p => p.Name)
            .Select(p => new PortfolioListDto(
                p.Id.Value, p.Name, p.Code, p.Description,
                p.Status.ToString(), p.OwnerUserId,
                p.Projects.Count, p.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetPortfolioQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetPortfolioQuery, PortfolioDetailDto?>
{
    public async Task<PortfolioDetailDto?> Handle(GetPortfolioQuery request, CancellationToken ct)
    {
        var p = await db.Portfolios.Include(x => x.Projects)
            .FirstOrDefaultAsync(x => x.Id.Value == request.Id, ct);
        if (p is null) return null;
        return new PortfolioDetailDto(
            p.Id.Value, p.Name, p.Code, p.Description,
            p.Status.ToString(), p.OwnerUserId,
            p.CreatedAt, p.UpdatedAt,
            p.Projects.Select(pr => new ProjectListDto(
                pr.Id.Value, pr.Key, pr.Name, pr.Description,
                pr.Status.ToString(), pr.Methodology.ToString(), pr.Category.ToString(),
                pr.StartDate, pr.EndDate, pr.TargetEndDate,
                pr.ManagerUserId, pr.OwnerUserId,
                pr.PortfolioId.HasValue ? pr.PortfolioId.Value.Value : null, p.Name,
                pr.Tasks.Count, pr.CreatedAt)).ToList());
    }
}

// ── Application Queries ─────────────────────────────────────
public sealed class ListApplicationsQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListApplicationsQuery, List<ApplicationListDto>>
{
    public async Task<List<ApplicationListDto>> Handle(ListApplicationsQuery request, CancellationToken ct)
    {
        var query = db.Applications.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<CIStatus>(request.Status, out var s))
            query = query.Where(a => a.Status == s);
        if (!string.IsNullOrEmpty(request.ApplicationType) && Enum.TryParse<ApplicationType>(request.ApplicationType, out var t))
            query = query.Where(a => a.ApplicationType == t);
        return await query.OrderBy(a => a.Name)
            .Select(a => new ApplicationListDto(
                a.Id.Value, a.Name, a.Code, a.Description,
                a.ApplicationType.ToString(), a.Status.ToString(), a.Criticality.ToString(),
                a.OwnerUserId, a.TechLeadUserId,
                a.TechnologyStack, a.CurrentVersion,
                a.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetApplicationQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetApplicationQuery, ApplicationDetailDto?>
{
    public async Task<ApplicationDetailDto?> Handle(GetApplicationQuery request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.Id);
        var a = await db.Applications.FirstOrDefaultAsync(x => x.Id == ciId, ct);
        if (a is null) return null;

        // Deliverables'ı ayrı sorguyla yükle (TPT include chain sorununu önler)
        var projects = await db.ProjectDeliverables
            .Include(d => d.Project)
            .Where(d => d.ConfigurationItemId == ciId)
            .Select(d => new CIProjectDto(
                d.ProjectId.Value, d.Project!.Key, d.Project.Name,
                d.Project.Status.ToString(), d.Role.ToString(), d.Notes))
            .ToListAsync(ct);

        return new ApplicationDetailDto(
            a.Id.Value, a.Name, a.Code, a.Description,
            a.ApplicationType.ToString(), a.Status.ToString(), a.Criticality.ToString(),
            a.OwnerUserId, a.TechLeadUserId,
            a.TechnologyStack, a.RepositoryUrl, a.DocumentationUrl, a.CurrentVersion,
            a.CreatedAt, a.UpdatedAt, projects);
    }
}

// ── Application Commands ────────────────────────────────────
public sealed class CreateApplicationCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateApplicationCommand, Guid>
{
    public async Task<Guid> Handle(CreateApplicationCommand request, CancellationToken ct)
    {
        Enum.TryParse<ApplicationType>(request.ApplicationType, out var appType);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        var app = ApplicationBase.Create(request.Name, request.Code, request.Description,
            appType, crit, request.OwnerUserId, request.TechLeadUserId,
            request.TechnologyStack, request.RepositoryUrl, request.DocumentationUrl,
            request.CurrentVersion);
        db.Applications.Add(app);
        await db.SaveChangesAsync(ct);
        return app.Id.Value;
    }
}

public sealed class UpdateApplicationCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdateApplicationCommand, Guid>
{
    public async Task<Guid> Handle(UpdateApplicationCommand request, CancellationToken ct)
    {
        var app = await db.Applications.FindAsync([new ConfigurationItemId(request.ApplicationId)], ct)
            ?? throw new KeyNotFoundException($"Application {request.ApplicationId} not found");
        Enum.TryParse<ApplicationType>(request.ApplicationType, out var appType);
        Enum.TryParse<CIStatus>(request.Status, out var status);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        app.Update(request.Name, request.Description,
            request.ApplicationType is not null ? appType : null,
            request.Status is not null ? status : null,
            request.Criticality is not null ? crit : null,
            request.OwnerUserId, request.TechLeadUserId,
            request.TechnologyStack, request.RepositoryUrl,
            request.DocumentationUrl, request.CurrentVersion);
        await db.SaveChangesAsync(ct);
        return app.Id.Value;
    }
}

// ── Project Queries ─────────────────────────────────────────
public sealed class ListProjectsQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListProjectsQuery, List<ProjectListDto>>
{
    public async Task<List<ProjectListDto>> Handle(ListProjectsQuery request, CancellationToken ct)
    {
        var query = db.Projects.Include(p => p.Portfolio).AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ProjectStatus>(request.Status, out var s))
            query = query.Where(p => p.Status == s);
        if (request.PortfolioId.HasValue)
            query = query.Where(p => p.PortfolioId.HasValue && p.PortfolioId.Value.Value == request.PortfolioId.Value);
        return await query.OrderBy(p => p.Name)
            .Select(p => new ProjectListDto(
                p.Id.Value, p.Key, p.Name, p.Description,
                p.Status.ToString(), p.Methodology.ToString(), p.Category.ToString(),
                p.StartDate, p.EndDate, p.TargetEndDate,
                p.ManagerUserId, p.OwnerUserId,
                p.PortfolioId.HasValue ? p.PortfolioId.Value.Value : null,
                p.Portfolio != null ? p.Portfolio.Name : null,
                p.Tasks.Count, p.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetProjectQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetProjectQuery, ProjectDetailDto?>
{
    public async Task<ProjectDetailDto?> Handle(GetProjectQuery request, CancellationToken ct)
    {
        var projectId = new ProjectId(request.Id);
        var p = await db.Projects
            .Include(x => x.Portfolio)
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == projectId, ct);
        if (p is null) return null;

        // Deliverables'ı ayrı sorguyla yükle
        var deliverables = await db.ProjectDeliverables
            .Include(d => d.ConfigurationItem)
            .Where(d => d.ProjectId == projectId)
            .Select(d => new ProjectDeliverableDto(
                d.Id.Value, d.ConfigurationItemId.Value,
                d.ConfigurationItem!.Name, d.ConfigurationItem.Code,
                "Application",
                d.Role.ToString(), d.Notes))
            .ToListAsync(ct);

        return new ProjectDetailDto(
            p.Id.Value, p.Key, p.Name, p.Description,
            p.Status.ToString(), p.Methodology.ToString(), p.Category.ToString(),
            p.StartDate, p.EndDate, p.TargetEndDate,
            p.ManagerUserId, p.OwnerUserId,
            p.PortfolioId.HasValue ? p.PortfolioId.Value.Value : null,
            p.Portfolio?.Name, p.Portfolio?.Code,
            p.Tasks.Count, p.TaskSequence,
            p.CreatedAt, p.UpdatedAt, deliverables);
    }
}

// ── Portfolio Commands ──────────────────────────────────────
public sealed class CreatePortfolioCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreatePortfolioCommand, Guid>
{
    public async Task<Guid> Handle(CreatePortfolioCommand request, CancellationToken ct)
    {
        var portfolio = PortfolioBase.Create(request.Name, request.Code,
            request.Description, request.OwnerUserId);
        db.Portfolios.Add(portfolio);
        await db.SaveChangesAsync(ct);
        return portfolio.Id.Value;
    }
}

public sealed class UpdatePortfolioCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdatePortfolioCommand, Guid>
{
    public async Task<Guid> Handle(UpdatePortfolioCommand request, CancellationToken ct)
    {
        var portfolio = await db.Portfolios.FindAsync([new PortfolioId(request.PortfolioId)], ct)
            ?? throw new KeyNotFoundException($"Portfolio {request.PortfolioId} not found");
        Enum.TryParse<PortfolioStatus>(request.Status, out var status);
        portfolio.Update(request.Name, request.Code, request.Description,
            request.OwnerUserId, request.Status is not null ? status : null);
        await db.SaveChangesAsync(ct);
        return portfolio.Id.Value;
    }
}

// ── Project Commands ────────────────────────────────────────
public sealed class CreateProjectCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateProjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        Enum.TryParse<ProjectMethodology>(request.Methodology, out var methodology);
        Enum.TryParse<ProjectCategory>(request.Category, out var category);
        var project = ProjectBase.Create(request.Key, request.Name, request.Description,
            request.StartDate, request.EndDate, request.TargetEndDate,
            request.ManagerUserId, request.OwnerUserId,
            request.PortfolioId.HasValue ? new PortfolioId(request.PortfolioId.Value) : null,
            methodology, category);
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return project.Id.Value;
    }
}

public sealed class UpdateProjectCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdateProjectCommand, Guid>
{
    public async Task<Guid> Handle(UpdateProjectCommand request, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([new ProjectId(request.ProjectId)], ct)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found");
        Enum.TryParse<ProjectStatus>(request.Status, out var status);
        Enum.TryParse<ProjectMethodology>(request.Methodology, out var methodology);
        Enum.TryParse<ProjectCategory>(request.Category, out var category);
        project.Update(request.Name, request.Description,
            request.StartDate, request.EndDate, request.TargetEndDate,
            request.ManagerUserId, request.OwnerUserId,
            request.PortfolioId.HasValue ? new PortfolioId(request.PortfolioId.Value) : null,
            request.Status is not null ? status : null,
            request.Methodology is not null ? methodology : null,
            request.Category is not null ? category : null);
        await db.SaveChangesAsync(ct);
        return project.Id.Value;
    }
}

public sealed class ListTasksQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListTasksQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListTasksQuery request, CancellationToken ct)
    {
        var query = db.Tasks.Include(t => t.Project).AsQueryable();
        if (request.ProjectId.HasValue) query = query.Where(t => t.ProjectId.HasValue && t.ProjectId.Value.Value == request.ProjectId.Value);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TaskStatusEnum>(request.Status, out var s))
            query = query.Where(t => t.Status == s);
        if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<TaskPriority>(request.Priority, out var p))
            query = query.Where(t => t.Priority == p);
        if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<TaskType>(request.Type, out var tp))
            query = query.Where(t => t.Type == tp);
        if (Guid.TryParse(request.Assignee, out var uid))
            query = query.Where(t => t.AssigneeUserId == uid);
        if (request.ReporterUserId.HasValue)
            query = query.Where(t => t.ReporterUserId == request.ReporterUserId.Value);

        // Çoklu assignee filtresi (virgülle ayrılmış Guid listesi)
        if (!string.IsNullOrEmpty(request.AssigneeUserIds))
        {
            var assigneeIds = request.AssigneeUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Guid.TryParse(x.Trim(), out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            if (assigneeIds.Count > 0)
                query = query.Where(t => t.AssigneeUserId.HasValue && assigneeIds.Contains(t.AssigneeUserId.Value));
        }

        // Kaynak filtresi: "independent" (bağımsız), "ticket" (talep), "project" (proje)
        if (!string.IsNullOrEmpty(request.SourceFilter))
        {
            query = request.SourceFilter.ToLower() switch
            {
                "independent" => query.Where(t => t.SourceId == null && !t.ProjectId.HasValue),
                "ticket" => query.Where(t => t.SourceModule == "RequestManagement"),
                "project" => query.Where(t => t.ProjectId.HasValue),
                _ => query
            };
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(t => (object)new { t.Id, t.TaskNumber, t.Title, ProjectKey = t.Project != null ? t.Project.Key : null,
                Status = t.Status.ToString(), Priority = t.Priority.ToString(),
                Type = t.Type.ToString(), t.AssigneeUserId, t.ReporterUserId, t.DueDate, t.EstimatedHours, t.SortOrder, t.ParentTaskId,
                t.SourceModule, t.SourceType, t.SourceId, t.CreatedAt })
            .ToListAsync(ct);
        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetTaskQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetTaskQuery, TaskDetailDto?>
{
    public async Task<TaskDetailDto?> Handle(GetTaskQuery request, CancellationToken ct)
    {
        var t = await db.Tasks.Include(x => x.SubTasks).Include(x => x.Project).Include(x => x.Sprint)
            .FirstOrDefaultAsync(x => x.Id.Value == request.Id, ct);
        if (t is null) return null;
        return new TaskDetailDto(
            t.Id.Value, t.TaskNumber, t.Title, t.Description,
            t.Status.ToString(), t.Priority.ToString(), t.Type.ToString(),
            t.AssigneeUserId, t.ReporterUserId,
            t.ParentTaskId.HasValue ? t.ParentTaskId.Value.Value : null,
            t.DueDate, t.EstimatedHours, t.SortOrder, t.Tags,
            t.SourceModule, t.SourceType, t.SourceId,
            t.ProjectId.HasValue ? t.ProjectId.Value.Value : null,
            t.Project?.Key, t.Project?.Name,
            t.CreatedAt, t.UpdatedAt,
            t.SubTasks.Select(st => new TaskBySourceDto(
                st.Id.Value, st.TaskNumber, st.Title, st.Status.ToString(),
                st.Priority.ToString(), st.Type.ToString(), st.AssigneeUserId,
                st.DueDate, st.EstimatedHours, st.CreatedAt,
                st.StoryPoints, st.HierarchyLevel)).ToList(),
            t.StoryPoints, t.AcceptanceCriteria,
            t.SprintId.HasValue ? t.SprintId.Value.Value : null,
            t.Sprint?.Name, t.HierarchyLevel);
    }
}

public sealed class GetKanbanBoardQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetKanbanBoardQuery, object>
{
    public async Task<object> Handle(GetKanbanBoardQuery request, CancellationToken ct)
    {
        var tasks = await db.Tasks.Where(t => t.ProjectId.HasValue && t.ProjectId.Value.Value == request.ProjectId)
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Id, t.TaskNumber, t.Title, Status = t.Status.ToString(),
                Priority = t.Priority.ToString(), Type = t.Type.ToString(),
                t.AssigneeUserId, t.SortOrder, t.DueDate })
            .ToListAsync(ct);
        return tasks.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.ToList());
    }
}

public sealed class ListCommentsQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListCommentsQuery, List<object>>
{
    public async Task<List<object>> Handle(ListCommentsQuery request, CancellationToken ct)
        => await db.Comments.Where(c => c.TaskId.Value == request.TaskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => (object)new { c.Id, c.AuthorUserId, c.Content, c.CreatedAt })
            .ToListAsync(ct);
}

public sealed class ListTimeEntriesQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListTimeEntriesQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListTimeEntriesQuery request, CancellationToken ct)
    {
        var query = db.TimeEntries.AsQueryable();
        if (request.TaskId.HasValue) query = query.Where(t => t.TaskId.Value == request.TaskId.Value);
        if (request.UserId.HasValue) query = query.Where(t => t.UserId == request.UserId.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.WorkDate)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(t => (object)new { t.Id, t.TaskId, t.UserId, t.Hours, t.WorkDate, t.Description })
            .ToListAsync(ct);
        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

/// <summary>Kaynağa bağlı görevleri listeler (Ticket detay sayfası için).</summary>
public sealed class ListTasksBySourceQueryHandler(TaskManagementDbContext db)
    : IRequestHandler<ListTasksBySourceQuery, List<TaskBySourceDto>>
{
    public async Task<List<TaskBySourceDto>> Handle(ListTasksBySourceQuery request, CancellationToken ct)
    {
        return await db.Tasks
            .Where(t => t.SourceModule == request.SourceModule
                && t.SourceType == request.SourceType
                && t.SourceId == request.SourceId)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.CreatedAt)
            .Select(t => new TaskBySourceDto(
                t.Id.Value, t.TaskNumber, t.Title, t.Status.ToString(),
                t.Priority.ToString(), t.Type.ToString(), t.AssigneeUserId,
                t.DueDate, t.EstimatedHours, t.CreatedAt,
                t.StoryPoints, t.HierarchyLevel))
            .ToListAsync(ct);
    }
}

// ── Task Commands ───────────────────────────────────────────
public sealed class CreateTaskCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateTaskCommand, CreateTaskResult>
{
    public async Task<CreateTaskResult> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        Enum.TryParse<TaskType>(request.Type, out var type);
        Enum.TryParse<TaskPriority>(request.Priority, out var priority);

        TaskItemBase task;
        if (request.ProjectId.HasValue)
        {
            var project = await db.Projects.FindAsync([new ProjectId(request.ProjectId.Value)], ct)
                ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found");
            var taskNumber = project.NextTaskNumber();
            task = TaskItemBase.Create(project.Id, taskNumber, request.Title, type, priority,
                request.Description, request.AssigneeUserId, request.ReporterUserId,
                request.ParentTaskId.HasValue ? new TaskItemId(request.ParentTaskId.Value) : null,
                request.DueDate, request.EstimatedHours, request.Tags);
        }
        else
        {
            // Projesiz görev
            var taskNumber = await TaskNumberGenerator.NextAsync(db, ct);
            task = TaskItemBase.CreateStandalone(taskNumber, request.Title, type, priority,
                request.Description, request.AssigneeUserId, request.ReporterUserId,
                request.DueDate, request.EstimatedHours, request.Tags);
        }

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return new CreateTaskResult(task.Id.Value, task.TaskNumber);
    }
}

/// <summary>Dış kaynaktan (Ticket vb.) görev oluşturur ve integration event publish eder.</summary>
public sealed class CreateTaskFromSourceCommandHandler(TaskManagementDbContext db, IEventBus eventBus)
    : IRequestHandler<CreateTaskFromSourceCommand, CreateTaskResult>
{
    public async Task<CreateTaskResult> Handle(CreateTaskFromSourceCommand request, CancellationToken ct)
    {
        Enum.TryParse<TaskPriority>(request.Priority, out var priority);
        var taskNumber = await TaskNumberGenerator.NextAsync(db, ct);

        var task = TaskItemBase.CreateFromSource(
            request.SourceModule, request.SourceType, request.SourceId,
            taskNumber, request.Title,
            priority: priority,
            description: request.Description,
            assigneeUserId: request.AssigneeUserId,
            reporterUserId: request.ReporterUserId,
            dueDate: request.DueDate,
            projectId: request.ProjectId.HasValue ? new ProjectId(request.ProjectId.Value) : null);

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);

        // Integration event — RequestManagement bu event'i dinleyerek LinkedTaskCount'u artıracak
        await eventBus.PublishAsync(new TaskCreatedForSourceEvent(
            task.Id.Value, task.TaskNumber,
            request.SourceModule, request.SourceType, request.SourceId,
            request.AssigneeUserId), ct);

        return new CreateTaskResult(task.Id.Value, task.TaskNumber);
    }
}

public sealed class MoveTaskCommandHandler(TaskManagementDbContext db, IEventBus eventBus)
    : IRequestHandler<MoveTaskCommand, MoveTaskResult>
{
    public async Task<MoveTaskResult> Handle(MoveTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([new TaskItemId(request.TaskId)], ct)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");
        if (!Enum.TryParse<TaskStatusEnum>(request.Status, out var status))
            throw new ArgumentException($"Invalid status: {request.Status}");

        var oldStatus = task.Status;
        task.MoveTo(status);
        if (request.SortOrder.HasValue) task.SetSortOrder(request.SortOrder.Value);
        await db.SaveChangesAsync(ct);

        // Integration event — durum değişikliği
        if (task.HasSource)
        {
            await eventBus.PublishAsync(new TaskStatusChangedEvent(
                task.Id.Value, task.TaskNumber,
                oldStatus.ToString(), status.ToString(),
                task.SourceModule, task.SourceType, task.SourceId), ct);

            // Görev Done veya Cancelled'a geçtiyse — aynı kaynağa bağlı tüm görevleri kontrol et
            if (status is TaskStatusEnum.Done or TaskStatusEnum.Cancelled)
            {
                await CheckAllSourceTasksCompleted(task, ct);
            }
        }

        return new MoveTaskResult(task.Id.Value, task.Status.ToString(), task.SortOrder);
    }

    private async Task CheckAllSourceTasksCompleted(TaskItemBase completedTask, CancellationToken ct)
    {
        if (completedTask.SourceModule is null || completedTask.SourceType is null || !completedTask.SourceId.HasValue)
            return;

        var sourceTasks = await db.Tasks
            .Where(t => t.SourceModule == completedTask.SourceModule
                && t.SourceType == completedTask.SourceType
                && t.SourceId == completedTask.SourceId)
            .Select(t => t.Status)
            .ToListAsync(ct);

        var allDone = sourceTasks.All(s => s is TaskStatusEnum.Done or TaskStatusEnum.Cancelled);
        if (allDone && sourceTasks.Count > 0)
        {
            await eventBus.PublishAsync(new AllSourceTasksCompletedEvent(
                completedTask.SourceModule, completedTask.SourceType, completedTask.SourceId.Value,
                sourceTasks.Count), ct);
        }
    }
}

public sealed class AssignTaskCommandHandler(TaskManagementDbContext db) : IRequestHandler<AssignTaskCommand, Guid?>
{
    public async Task<Guid?> Handle(AssignTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([new TaskItemId(request.TaskId)], ct)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");
        if (request.UserId.HasValue)
            task.AssignTo(request.UserId.Value);
        else
            task.Unassign();
        await db.SaveChangesAsync(ct);
        return task.AssigneeUserId;
    }
}

public sealed class CreateCommentCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateCommentCommand, Guid>
{
    public async Task<Guid> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var comment = CommentBase.Create(new TaskItemId(request.TaskId), request.AuthorUserId, request.Content);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment.Id.Value;
    }
}

public sealed class CreateTimeEntryCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateTimeEntryCommand, Guid>
{
    public async Task<Guid> Handle(CreateTimeEntryCommand request, CancellationToken ct)
    {
        var entry = TimeEntryBase.Create(new TaskItemId(request.TaskId), request.UserId, request.Hours,
            request.WorkDate, request.Description);
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id.Value;
    }
}

public sealed class UpdateTaskCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdateTaskCommand, Guid>
{
    public async Task<Guid> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([new TaskItemId(request.TaskId)], ct)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        Enum.TryParse<TaskPriority>(request.Priority, out var priority);
        Enum.TryParse<TaskType>(request.Type, out var type);

        task.Update(
            title: request.Title,
            description: request.Description,
            priority: request.Priority is not null ? priority : null,
            type: request.Type is not null ? type : null,
            dueDate: request.DueDate,
            estimatedHours: request.EstimatedHours,
            tags: request.Tags,
            storyPoints: request.StoryPoints,
            acceptanceCriteria: request.AcceptanceCriteria);

        if (request.AssigneeUserId.HasValue)
            task.AssignTo(request.AssigneeUserId.Value);

        if (request.SprintId.HasValue)
            task.AssignToSprint(new SprintId(request.SprintId.Value));

        await db.SaveChangesAsync(ct);
        return task.Id.Value;
    }
}

// ── ProjectDeliverable Handlers ───────────────────────────────
public sealed class AddProjectDeliverableCommandHandler(TaskManagementDbContext db)
    : IRequestHandler<AddProjectDeliverableCommand, Guid>
{
    public async Task<Guid> Handle(AddProjectDeliverableCommand request, CancellationToken ct)
    {
        var projectId = new ProjectId(request.ProjectId);
        var ciId = new ConfigurationItemId(request.ConfigurationItemId);

        // Duplicate check
        var exists = await db.ProjectDeliverables
            .AnyAsync(d => d.ProjectId == projectId && d.ConfigurationItemId == ciId, ct);
        if (exists)
            throw new InvalidOperationException("Bu CI zaten projenin deliverable listesinde.");

        Enum.TryParse<DeliverableRole>(request.Role, out var role);
        var deliverable = ProjectDeliverable.Create(projectId, ciId, role, request.Notes);
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync(ct);
        return deliverable.Id.Value;
    }
}

public sealed class RemoveProjectDeliverableCommandHandler(TaskManagementDbContext db)
    : IRequestHandler<RemoveProjectDeliverableCommand>
{
    public async Task Handle(RemoveProjectDeliverableCommand request, CancellationToken ct)
    {
        var projectId = new ProjectId(request.ProjectId);
        var ciId = new ConfigurationItemId(request.ConfigurationItemId);
        var deliverable = await db.ProjectDeliverables
            .FirstOrDefaultAsync(d => d.ProjectId == projectId && d.ConfigurationItemId == ciId, ct)
            ?? throw new KeyNotFoundException("Deliverable not found.");
        db.ProjectDeliverables.Remove(deliverable);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ListProjectDeliverablesQueryHandler(TaskManagementDbContext db)
    : IRequestHandler<ListProjectDeliverablesQuery, List<ProjectDeliverableDto>>
{
    public async Task<List<ProjectDeliverableDto>> Handle(ListProjectDeliverablesQuery request, CancellationToken ct)
    {
        return await db.ProjectDeliverables
            .Include(d => d.ConfigurationItem)
            .Where(d => d.ProjectId.Value == request.ProjectId)
            .OrderBy(d => d.Role)
            .Select(d => new ProjectDeliverableDto(
                d.Id.Value, d.ConfigurationItemId.Value,
                d.ConfigurationItem!.Name, d.ConfigurationItem.Code,
                d.ConfigurationItem is ApplicationBase ? "Application" : "CI",
                d.Role.ToString(), d.Notes))
            .ToListAsync(ct);
    }
}

public sealed class ListCIProjectsQueryHandler(TaskManagementDbContext db)
    : IRequestHandler<ListCIProjectsQuery, List<CIProjectDto>>
{
    public async Task<List<CIProjectDto>> Handle(ListCIProjectsQuery request, CancellationToken ct)
    {
        return await db.ProjectDeliverables
            .Include(d => d.Project)
            .Where(d => d.ConfigurationItemId.Value == request.ConfigurationItemId)
            .OrderBy(d => d.Project!.Name)
            .Select(d => new CIProjectDto(
                d.ProjectId.Value, d.Project!.Key, d.Project.Name,
                d.Project.Status.ToString(), d.Role.ToString(), d.Notes))
            .ToListAsync(ct);
    }
}

// ══════════════════════════════════════════════════════════════
// SERVER HANDLERS
// ══════════════════════════════════════════════════════════════

public sealed class ListServersQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListServersQuery, List<ServerListDto>>
{
    public async Task<List<ServerListDto>> Handle(ListServersQuery request, CancellationToken ct)
    {
        var query = db.Servers.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<CIStatus>(request.Status, out var s))
            query = query.Where(x => x.Status == s);
        if (!string.IsNullOrEmpty(request.ServerType) && Enum.TryParse<ServerType>(request.ServerType, out var st))
            query = query.Where(x => x.ServerType == st);
        if (!string.IsNullOrEmpty(request.Environment) && Enum.TryParse<DeploymentEnvironment>(request.Environment, out var env))
            query = query.Where(x => x.Environment == env);
        return await query.OrderBy(x => x.Name)
            .Select(x => new ServerListDto(
                x.Id.Value, x.Name, x.Code, x.Description,
                x.ServerType.ToString(), x.Environment.ToString(), x.Status.ToString(), x.Criticality.ToString(),
                x.OperatingSystem, x.IpAddress, x.Hostname,
                x.CpuCores, x.RamGB, x.DiskGB, x.DataCenter,
                x.OwnerUserId, x.AdminUserId, x.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetServerQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetServerQuery, ServerDetailDto?>
{
    public async Task<ServerDetailDto?> Handle(GetServerQuery request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.Id);
        var s = await db.Servers.FirstOrDefaultAsync(x => x.Id == ciId, ct);
        if (s is null) return null;
        return new ServerDetailDto(
            s.Id.Value, s.Name, s.Code, s.Description,
            s.ServerType.ToString(), s.Environment.ToString(), s.Status.ToString(), s.Criticality.ToString(),
            s.OperatingSystem, s.IpAddress, s.Hostname,
            s.CpuCores, s.RamGB, s.DiskGB, s.DataCenter,
            s.OwnerUserId, s.AdminUserId, s.CreatedAt, s.UpdatedAt);
    }
}

public sealed class CreateServerCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateServerCommand, Guid>
{
    public async Task<Guid> Handle(CreateServerCommand request, CancellationToken ct)
    {
        Enum.TryParse<ServerType>(request.ServerType, out var st);
        Enum.TryParse<DeploymentEnvironment>(request.Environment, out var env);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        var server = ServerCI.Create(request.Name, request.Code, request.Description,
            st, env, crit, request.OwnerUserId, request.AdminUserId,
            request.OperatingSystem, request.IpAddress, request.Hostname,
            request.CpuCores, request.RamGB, request.DiskGB, request.DataCenter);
        db.Servers.Add(server);
        await db.SaveChangesAsync(ct);
        return server.Id.Value;
    }
}

public sealed class UpdateServerCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdateServerCommand, Guid>
{
    public async Task<Guid> Handle(UpdateServerCommand request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.ServerId);
        var server = await db.Servers.FirstOrDefaultAsync(x => x.Id == ciId, ct)
            ?? throw new InvalidOperationException("Server not found");
        Enum.TryParse<ServerType>(request.ServerType, out var st);
        Enum.TryParse<DeploymentEnvironment>(request.Environment, out var env);
        Enum.TryParse<CIStatus>(request.Status, out var status);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        server.Update(request.Name, request.Description,
            request.ServerType != null ? st : null, request.Environment != null ? env : null,
            request.Status != null ? status : null, request.Criticality != null ? crit : null,
            request.OwnerUserId, request.AdminUserId,
            request.OperatingSystem, request.IpAddress, request.Hostname,
            request.CpuCores, request.RamGB, request.DiskGB, request.DataCenter);
        await db.SaveChangesAsync(ct);
        return server.Id.Value;
    }
}

// ══════════════════════════════════════════════════════════════
// DATABASE HANDLERS
// ══════════════════════════════════════════════════════════════

public sealed class ListDatabasesQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListDatabasesQuery, List<DatabaseListDto>>
{
    public async Task<List<DatabaseListDto>> Handle(ListDatabasesQuery request, CancellationToken ct)
    {
        var query = db.Databases.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<CIStatus>(request.Status, out var s))
            query = query.Where(x => x.Status == s);
        if (!string.IsNullOrEmpty(request.DatabaseEngine) && Enum.TryParse<DatabaseEngine>(request.DatabaseEngine, out var eng))
            query = query.Where(x => x.DatabaseEngine == eng);
        return await query.OrderBy(x => x.Name)
            .Select(x => new DatabaseListDto(
                x.Id.Value, x.Name, x.Code, x.Description,
                x.DatabaseEngine.ToString(), x.Status.ToString(), x.Criticality.ToString(),
                x.Version, x.Port, x.SizeGB, x.BackupSchedule,
                x.OwnerUserId, x.AdminUserId, x.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetDatabaseQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetDatabaseQuery, DatabaseDetailDto?>
{
    public async Task<DatabaseDetailDto?> Handle(GetDatabaseQuery request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.Id);
        var d = await db.Databases.FirstOrDefaultAsync(x => x.Id == ciId, ct);
        if (d is null) return null;
        return new DatabaseDetailDto(
            d.Id.Value, d.Name, d.Code, d.Description,
            d.DatabaseEngine.ToString(), d.Status.ToString(), d.Criticality.ToString(),
            d.Version, d.Port, d.SizeGB, d.ConnectionString, d.BackupSchedule,
            d.OwnerUserId, d.AdminUserId, d.CreatedAt, d.UpdatedAt);
    }
}

public sealed class CreateDatabaseCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateDatabaseCommand, Guid>
{
    public async Task<Guid> Handle(CreateDatabaseCommand request, CancellationToken ct)
    {
        Enum.TryParse<DatabaseEngine>(request.DatabaseEngine, out var eng);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        var database = DatabaseCI.Create(request.Name, request.Code, request.Description,
            eng, crit, request.OwnerUserId, request.AdminUserId,
            request.Version, request.Port, request.SizeGB,
            request.ConnectionString, request.BackupSchedule);
        db.Databases.Add(database);
        await db.SaveChangesAsync(ct);
        return database.Id.Value;
    }
}

public sealed class UpdateDatabaseCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdateDatabaseCommand, Guid>
{
    public async Task<Guid> Handle(UpdateDatabaseCommand request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.DatabaseId);
        var database = await db.Databases.FirstOrDefaultAsync(x => x.Id == ciId, ct)
            ?? throw new InvalidOperationException("Database not found");
        Enum.TryParse<DatabaseEngine>(request.DatabaseEngine, out var eng);
        Enum.TryParse<CIStatus>(request.Status, out var status);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        database.Update(request.Name, request.Description,
            request.DatabaseEngine != null ? eng : null,
            request.Status != null ? status : null, request.Criticality != null ? crit : null,
            request.OwnerUserId, request.AdminUserId,
            request.Version, request.Port, request.SizeGB,
            request.ConnectionString, request.BackupSchedule);
        await db.SaveChangesAsync(ct);
        return database.Id.Value;
    }
}

// ══════════════════════════════════════════════════════════════
// LICENCE HANDLERS
// ══════════════════════════════════════════════════════════════

public sealed class ListLicencesQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListLicencesQuery, List<LicenceListDto>>
{
    public async Task<List<LicenceListDto>> Handle(ListLicencesQuery request, CancellationToken ct)
    {
        var query = db.Licences.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<CIStatus>(request.Status, out var s))
            query = query.Where(x => x.Status == s);
        if (!string.IsNullOrEmpty(request.LicenceType) && Enum.TryParse<LicenceType>(request.LicenceType, out var lt))
            query = query.Where(x => x.LicenceType == lt);
        return await query.OrderBy(x => x.Name)
            .Select(x => new LicenceListDto(
                x.Id.Value, x.Name, x.Code, x.Description,
                x.LicenceType.ToString(), x.Status.ToString(), x.Criticality.ToString(),
                x.Vendor, x.ProductName,
                x.MaxUsers, x.CurrentUsers,
                x.ExpirationDate, x.AnnualCost, x.Currency,
                x.OwnerUserId, x.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetLicenceQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetLicenceQuery, LicenceDetailDto?>
{
    public async Task<LicenceDetailDto?> Handle(GetLicenceQuery request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.Id);
        var l = await db.Licences.FirstOrDefaultAsync(x => x.Id == ciId, ct);
        if (l is null) return null;
        return new LicenceDetailDto(
            l.Id.Value, l.Name, l.Code, l.Description,
            l.LicenceType.ToString(), l.Status.ToString(), l.Criticality.ToString(),
            l.Vendor, l.ProductName, l.LicenceKey,
            l.MaxUsers, l.CurrentUsers,
            l.ExpirationDate, l.PurchaseDate,
            l.AnnualCost, l.Currency,
            l.OwnerUserId, l.CreatedAt, l.UpdatedAt);
    }
}

public sealed class CreateLicenceCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateLicenceCommand, Guid>
{
    public async Task<Guid> Handle(CreateLicenceCommand request, CancellationToken ct)
    {
        Enum.TryParse<LicenceType>(request.LicenceType, out var lt);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        var licence = LicenceCI.Create(request.Name, request.Code, request.Description,
            lt, crit, request.OwnerUserId,
            request.Vendor, request.ProductName, request.LicenceKey,
            request.MaxUsers, request.CurrentUsers,
            request.ExpirationDate, request.PurchaseDate,
            request.AnnualCost, request.Currency);
        db.Licences.Add(licence);
        await db.SaveChangesAsync(ct);
        return licence.Id.Value;
    }
}

public sealed class UpdateLicenceCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdateLicenceCommand, Guid>
{
    public async Task<Guid> Handle(UpdateLicenceCommand request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.LicenceId);
        var licence = await db.Licences.FirstOrDefaultAsync(x => x.Id == ciId, ct)
            ?? throw new InvalidOperationException("Licence not found");
        Enum.TryParse<LicenceType>(request.LicenceType, out var lt);
        Enum.TryParse<CIStatus>(request.Status, out var status);
        Enum.TryParse<CICriticality>(request.Criticality, out var crit);
        licence.Update(request.Name, request.Description,
            request.LicenceType != null ? lt : null,
            request.Status != null ? status : null, request.Criticality != null ? crit : null,
            request.OwnerUserId,
            request.Vendor, request.ProductName, request.LicenceKey,
            request.MaxUsers, request.CurrentUsers,
            request.ExpirationDate, request.PurchaseDate,
            request.AnnualCost, request.Currency);
        await db.SaveChangesAsync(ct);
        return licence.Id.Value;
    }
}

// ══════════════════════════════════════════════════════════════
// CI RELATIONSHIP HANDLERS
// ══════════════════════════════════════════════════════════════

public sealed class AddCIRelationshipCommandHandler(TaskManagementDbContext db) : IRequestHandler<AddCIRelationshipCommand, Guid>
{
    public async Task<Guid> Handle(AddCIRelationshipCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CIRelationType>(request.RelationType, out var relType))
            throw new InvalidOperationException($"Invalid relation type: {request.RelationType}");
        var sourceCIId = new ConfigurationItemId(request.SourceCIId);
        var targetCIId = new ConfigurationItemId(request.TargetCIId);
        // Self-reference check
        if (sourceCIId == targetCIId)
            throw new InvalidOperationException("Cannot create a relationship between a CI and itself");
        // Verify both CIs exist
        var sourceExists = await db.Set<ConfigurationItemBase>().AnyAsync(ci => ci.Id == sourceCIId, ct);
        var targetExists = await db.Set<ConfigurationItemBase>().AnyAsync(ci => ci.Id == targetCIId, ct);
        if (!sourceExists) throw new InvalidOperationException("Source CI not found");
        if (!targetExists) throw new InvalidOperationException("Target CI not found");
        // Check duplicate
        var exists = await db.CIRelationships.AnyAsync(r =>
            r.SourceCIId == sourceCIId && r.TargetCIId == targetCIId && r.RelationType == relType, ct);
        if (exists) throw new InvalidOperationException("Relationship already exists");
        var rel = CIRelationship.Create(sourceCIId, targetCIId, relType, request.Notes);
        db.CIRelationships.Add(rel);
        await db.SaveChangesAsync(ct);
        return rel.Id.Value;
    }
}

public sealed class RemoveCIRelationshipCommandHandler(TaskManagementDbContext db) : IRequestHandler<RemoveCIRelationshipCommand>
{
    public async Task Handle(RemoveCIRelationshipCommand request, CancellationToken ct)
    {
        var relId = new CIRelationshipId(request.RelationshipId);
        var rel = await db.CIRelationships.FirstOrDefaultAsync(x => x.Id == relId, ct)
            ?? throw new InvalidOperationException("Relationship not found");
        db.CIRelationships.Remove(rel);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ListCIRelationshipsQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListCIRelationshipsQuery, List<CIRelationshipDto>>
{
    public async Task<List<CIRelationshipDto>> Handle(ListCIRelationshipsQuery request, CancellationToken ct)
    {
        var ciId = new ConfigurationItemId(request.CIId);

        // Load outgoing relationships with target CI data
        var outgoingRels = await db.CIRelationships
            .Include(r => r.TargetCI)
            .Where(r => r.SourceCIId == ciId)
            .ToListAsync(ct);

        var outgoing = outgoingRels.Select(r => new CIRelationshipDto(
            r.Id.Value, r.SourceCIId.Value, "", "", "",
            r.TargetCIId.Value, r.TargetCI?.Name ?? "", r.TargetCI?.Code ?? "", GetCIType(r.TargetCI),
            r.RelationType.ToString(), r.Notes, "outgoing")).ToList();

        // Load incoming relationships with source CI data
        var incomingRels = await db.CIRelationships
            .Include(r => r.SourceCI)
            .Where(r => r.TargetCIId == ciId)
            .ToListAsync(ct);

        var incoming = incomingRels.Select(r => new CIRelationshipDto(
            r.Id.Value, r.SourceCIId.Value, r.SourceCI?.Name ?? "", r.SourceCI?.Code ?? "", GetCIType(r.SourceCI),
            r.TargetCIId.Value, "", "", "",
            r.RelationType.ToString(), r.Notes, "incoming")).ToList();

        return [..outgoing, ..incoming];
    }

    private static string GetCIType(ConfigurationItemBase? ci) => ci switch
    {
        ApplicationBase => "Application",
        ServerCI => "Server",
        DatabaseCI => "Database",
        LicenceCI => "Licence",
        _ => "CI"
    };
}

// ══════════════════════════════════════════════════════════════
// SPRINT HANDLERS
// ══════════════════════════════════════════════════════════════

public sealed class ListSprintsQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListSprintsQuery, List<SprintListDto>>
{
    public async Task<List<SprintListDto>> Handle(ListSprintsQuery request, CancellationToken ct)
    {
        var projectId = new ProjectId(request.ProjectId);
        var query = db.Sprints.Include(s => s.Project).Include(s => s.WorkItems)
            .Where(s => s.ProjectId == projectId);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<SprintStatus>(request.Status, out var status))
            query = query.Where(s => s.Status == status);
        return await query.OrderByDescending(s => s.StartDate)
            .Select(s => new SprintListDto(
                s.Id.Value, s.Name, s.Goal, s.Status.ToString(),
                s.StartDate, s.EndDate, s.CapacityPoints,
                s.ProjectId.Value, s.Project!.Key,
                s.WorkItems.Count,
                s.WorkItems.Where(w => w.StoryPoints.HasValue).Sum(w => w.StoryPoints),
                s.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetSprintQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetSprintQuery, SprintDetailDto?>
{
    public async Task<SprintDetailDto?> Handle(GetSprintQuery request, CancellationToken ct)
    {
        var sprintId = new SprintId(request.Id);
        var s = await db.Sprints.Include(x => x.Project).Include(x => x.WorkItems)
            .FirstOrDefaultAsync(x => x.Id == sprintId, ct);
        if (s is null) return null;
        return new SprintDetailDto(
            s.Id.Value, s.Name, s.Goal, s.Status.ToString(),
            s.StartDate, s.EndDate, s.CapacityPoints,
            s.ProjectId.Value, s.Project!.Key, s.Project.Name,
            s.WorkItems.Count,
            s.WorkItems.Where(w => w.StoryPoints.HasValue).Sum(w => w.StoryPoints),
            s.CreatedAt, s.UpdatedAt,
            s.WorkItems.OrderBy(w => w.SortOrder).Select(w => new TaskBySourceDto(
                w.Id.Value, w.TaskNumber, w.Title, w.Status.ToString(),
                w.Priority.ToString(), w.Type.ToString(), w.AssigneeUserId,
                w.DueDate, w.EstimatedHours, w.CreatedAt,
                w.StoryPoints, w.HierarchyLevel)).ToList());
    }
}

public sealed class CreateSprintCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateSprintCommand, Guid>
{
    public async Task<Guid> Handle(CreateSprintCommand request, CancellationToken ct)
    {
        var projectId = new ProjectId(request.ProjectId);
        _ = await db.Projects.FindAsync([projectId], ct)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found");
        var sprint = SprintBase.Create(projectId, request.Name,
            request.StartDate, request.EndDate, request.Goal, request.CapacityPoints);
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync(ct);
        return sprint.Id.Value;
    }
}

public sealed class UpdateSprintCommandHandler(TaskManagementDbContext db) : IRequestHandler<UpdateSprintCommand, Guid>
{
    public async Task<Guid> Handle(UpdateSprintCommand request, CancellationToken ct)
    {
        var sprint = await db.Sprints.FindAsync([new SprintId(request.SprintId)], ct)
            ?? throw new KeyNotFoundException($"Sprint {request.SprintId} not found");
        sprint.Update(request.Name, request.Goal, request.StartDate, request.EndDate, request.CapacityPoints);
        await db.SaveChangesAsync(ct);
        return sprint.Id.Value;
    }
}

public sealed class StartSprintCommandHandler(TaskManagementDbContext db) : IRequestHandler<StartSprintCommand, Guid>
{
    public async Task<Guid> Handle(StartSprintCommand request, CancellationToken ct)
    {
        var sprint = await db.Sprints.FindAsync([new SprintId(request.SprintId)], ct)
            ?? throw new KeyNotFoundException($"Sprint {request.SprintId} not found");
        // Aynı projede başka aktif sprint var mı kontrol et
        var hasActiveSprint = await db.Sprints
            .AnyAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active, ct);
        if (hasActiveSprint)
            throw new InvalidOperationException("Bu projede zaten aktif bir sprint var. Önce mevcut sprint'i tamamlayın.");
        sprint.Start();
        await db.SaveChangesAsync(ct);
        return sprint.Id.Value;
    }
}

public sealed class CompleteSprintCommandHandler(TaskManagementDbContext db) : IRequestHandler<CompleteSprintCommand, Guid>
{
    public async Task<Guid> Handle(CompleteSprintCommand request, CancellationToken ct)
    {
        var sprint = await db.Sprints.FindAsync([new SprintId(request.SprintId)], ct)
            ?? throw new KeyNotFoundException($"Sprint {request.SprintId} not found");
        sprint.Complete();
        await db.SaveChangesAsync(ct);
        return sprint.Id.Value;
    }
}

public sealed class AssignToSprintCommandHandler(TaskManagementDbContext db) : IRequestHandler<AssignToSprintCommand, Guid>
{
    public async Task<Guid> Handle(AssignToSprintCommand request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([new TaskItemId(request.TaskId)], ct)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");
        task.AssignToSprint(request.SprintId.HasValue ? new SprintId(request.SprintId.Value) : null);
        await db.SaveChangesAsync(ct);
        return task.Id.Value;
    }
}

// ══════════════════════════════════════════════════════════════
// BACKLOG HANDLER
// ══════════════════════════════════════════════════════════════

public sealed class GetBacklogQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetBacklogQuery, object>
{
    public async Task<object> Handle(GetBacklogQuery request, CancellationToken ct)
    {
        var projectId = new ProjectId(request.ProjectId);
        var query = db.Tasks.Where(t => t.ProjectId.HasValue && t.ProjectId == projectId);

        // Tip filtresi
        if (!string.IsNullOrEmpty(request.Type))
        {
            var types = request.Type.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Enum.TryParse<TaskType>(x.Trim(), out var tp) ? tp : (TaskType?)null)
                .Where(x => x.HasValue).Select(x => x!.Value).ToList();
            if (types.Count > 0)
                query = query.Where(t => types.Contains(t.Type));
        }

        // Sprint filtresi
        if (!string.IsNullOrEmpty(request.Sprint))
        {
            if (request.Sprint.Equals("current", StringComparison.OrdinalIgnoreCase))
            {
                var activeSprint = await db.Sprints
                    .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active, ct);
                if (activeSprint is not null)
                    query = query.Where(t => t.SprintId.HasValue && t.SprintId == activeSprint.Id);
            }
            else if (Guid.TryParse(request.Sprint, out var sprintGuid))
            {
                var sprintId = new SprintId(sprintGuid);
                query = query.Where(t => t.SprintId.HasValue && t.SprintId == sprintId);
            }
        }

        // Assignee filtresi
        if (request.Assignee.HasValue)
            query = query.Where(t => t.AssigneeUserId == request.Assignee.Value);

        // Status filtresi
        if (!string.IsNullOrEmpty(request.Status))
        {
            var statuses = request.Status.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Enum.TryParse<TaskStatusEnum>(x.Trim(), out var st) ? st : (TaskStatusEnum?)null)
                .Where(x => x.HasValue).Select(x => x!.Value).ToList();
            if (statuses.Count > 0)
                query = query.Where(t => statuses.Contains(t.Status));
        }

        var items = await query.OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id, t.TaskNumber, t.Title, t.Description,
                Status = t.Status.ToString(), Priority = t.Priority.ToString(),
                Type = t.Type.ToString(), t.AssigneeUserId, t.ReporterUserId,
                ParentTaskId = t.ParentTaskId.HasValue ? t.ParentTaskId.Value.Value : (Guid?)null,
                t.DueDate, t.EstimatedHours, t.SortOrder, t.Tags,
                t.StoryPoints, t.AcceptanceCriteria, t.HierarchyLevel,
                SprintId = t.SprintId.HasValue ? t.SprintId.Value.Value : (Guid?)null,
                t.CreatedAt
            }).ToListAsync(ct);

        if (request.View.Equals("tree", StringComparison.OrdinalIgnoreCase))
        {
            // Hiyerarşik ağaç oluştur
            var lookup = items.ToLookup(i => i.ParentTaskId);
            object BuildTree(Guid? parentId) => lookup[parentId].Select(item => new
            {
                item.Id, item.TaskNumber, item.Title, item.Status, item.Priority, item.Type,
                item.AssigneeUserId, item.DueDate, item.StoryPoints, item.HierarchyLevel,
                item.SortOrder, item.SprintId,
                Children = BuildTree(item.Id.Value)
            }).ToList();
            return BuildTree(null);
        }

        return items;
    }
}
