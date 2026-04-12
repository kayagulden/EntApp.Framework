using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.TaskManagement.Application.Commands;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Ticket assign edildiğinde assignee için otomatik görev oluşturur.
/// Basit taleplerde tek görev yeterlidir (talep ≈ görev).
/// Karmaşık taleplerde ilk "Analiz/Değerlendirme" görevi olarak kullanılır,
/// ek görevler sonra manuel eklenir.
///
/// Genellikle WaitForAllTasksDone aktivitesinden önce konulur:
///   [CreateTaskForAssignee] → [WaitForAllTasksDone] → [Resolve]
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Creates a task for the ticket assignee. Useful for simple tickets where ticket = task, " +
    "or as an initial assessment task for complex tickets.",
    DisplayName = "Create Task for Assignee")]
public sealed class CreateTaskForAssigneeActivity : CodeActivity
{
    [Input(Description = "The ticket ID to create a task for.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Task title. Leave empty to use the ticket title.")]
    public Input<string?> TaskTitle { get; set; } = default!;

    [Input(Description = "The user ID to assign the task to. If empty, ticket's assignee will be used from workflow variables.")]
    public Input<Guid?> AssigneeUserId { get; set; } = default!;

    [Input(Description = "Task priority.",
        UIHint = "dropdown",
        Options = new[] { "Low", "Medium", "High", "Critical" },
        DefaultValue = "Medium")]
    public Input<string> Priority { get; set; } = new("Medium");

    [Input(Description = "Optional task description.")]
    public Input<string?> Description { get; set; } = default!;

    [Input(Description = "Optional due date for the task.")]
    public Input<DateTime?> DueDate { get; set; } = default!;

    [Output(Description = "The created task ID.")]
    public Output<Guid> CreatedTaskId { get; set; } = default!;

    [Output(Description = "The created task number (e.g. TSK-00001).")]
    public Output<string> CreatedTaskNumber { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = context.Get(TicketId);
        var title = context.Get(TaskTitle);
        var assignee = context.Get(AssigneeUserId);
        var priority = context.Get(Priority) ?? "Medium";
        var description = context.Get(Description);
        var dueDate = context.Get(DueDate);

        // Title boşsa ticket başlığını kullan (workflow variable'dan)
        if (string.IsNullOrWhiteSpace(title))
            title = "Talep çözümü";

        var mediator = context.GetRequiredService<ISender>();

        var result = await mediator.Send(new CreateTaskFromSourceCommand(
            SourceModule: "RequestManagement",
            SourceType: "Ticket",
            SourceId: ticketId,
            Title: title,
            Description: description,
            AssigneeUserId: assignee,
            Priority: priority,
            DueDate: dueDate));

        context.Set(CreatedTaskId, result.Id);
        context.Set(CreatedTaskNumber, result.TaskNumber);

        await context.CompleteActivityAsync();
    }
}
