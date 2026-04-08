using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Queries;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Ticket detay bilgilerini getirir.
/// Workflow içinde conditional branching için kullanılır (priority, status vb. kontrolü).
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Retrieves ticket details for use in workflow expressions and branching.",
    DisplayName = "Get Ticket Details")]
public sealed class GetTicketDetailsActivity : CodeActivity
{
    [Input(Description = "The ticket ID to retrieve details for.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Output(Description = "Current ticket status.")]
    public Output<string> Status { get; set; } = default!;

    [Output(Description = "Ticket priority.")]
    public Output<string> Priority { get; set; } = default!;

    [Output(Description = "Ticket title.")]
    public Output<string> Title { get; set; } = default!;

    [Output(Description = "Category name.")]
    public Output<string> CategoryName { get; set; } = default!;

    [Output(Description = "Current queue name (if routed).")]
    public Output<string?> QueueName { get; set; } = default!;

    [Output(Description = "Assigned user ID (if assigned).")]
    public Output<Guid?> AssigneeUserId { get; set; } = default!;

    [Output(Description = "Reporter user ID.")]
    public Output<Guid> ReporterUserId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = context.Get(TicketId);

        var mediator = context.GetRequiredService<ISender>();
        var ticket = await mediator.Send(new GetTicketQuery(ticketId));

        if (ticket is null)
        {
            await context.CompleteActivityWithOutcomesAsync("NotFound");
            return;
        }

        context.Set(Status, ticket.Status.ToString());
        context.Set(Priority, ticket.Priority.ToString());
        context.Set(Title, ticket.Title);
        context.Set(CategoryName, ticket.Category?.Name ?? "Unknown");
        context.Set(QueueName, ticket.ServiceQueue?.Name);
        context.Set(AssigneeUserId, ticket.AssigneeUserId);
        context.Set(ReporterUserId, ticket.ReporterUserId);

        await context.CompleteActivityAsync();
    }
}
