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
        var t = await db.Tasks.Include(x => x.SubTasks).Include(x => x.Project)
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
                st.DueDate, st.EstimatedHours, st.CreatedAt)).ToList());
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
                t.DueDate, t.EstimatedHours, t.CreatedAt))
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
            tags: request.Tags);

        if (request.AssigneeUserId.HasValue)
            task.AssignTo(request.AssigneeUserId.Value);

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
