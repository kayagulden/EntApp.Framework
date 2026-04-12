using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Domain.Enums;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Ticket'ın durumunu değiştirir.
/// Elsa designer'da "Ticket Management" kategorisinde görünür.
/// </summary>
[Activity("EntApp", "Ticket Management", "Changes the status of a ticket.",
    DisplayName = "Change Ticket Status")]
public sealed class ChangeTicketStatusActivity : CodeActivity
{
    [Input(Description = "The ticket ID to change status for.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "The new status to set.",
        UIHint = "dropdown",
        Options = new[] { "New", "Open", "InProgress", "WaitingForInfo", "Escalated", "AllTasksDone", "Resolved", "Closed", "Cancelled", "Reopened" })]
    public Input<string> NewStatus { get; set; } = default!;

    [Input(Description = "Optional reason for the status change.")]
    public Input<string?> Reason { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = context.Get(TicketId);
        var newStatusStr = context.Get(NewStatus) ?? "New";
        var reason = context.Get(Reason);

        var newStatus = Enum.Parse<TicketStatus>(newStatusStr);

        var mediator = context.GetRequiredService<ISender>();
        await mediator.Send(new ChangeTicketStatusCommand(ticketId, newStatus, reason));

        await context.CompleteActivityAsync();
    }
}
