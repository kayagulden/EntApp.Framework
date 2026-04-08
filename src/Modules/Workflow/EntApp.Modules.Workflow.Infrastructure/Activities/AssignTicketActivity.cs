using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Commands;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Ticket'ı belirtilen kullanıcıya atar.
/// </summary>
[Activity("EntApp", "Ticket Management", "Assigns a ticket to a specific user.",
    DisplayName = "Assign Ticket")]
public sealed class AssignTicketActivity : CodeActivity
{
    [Input(Description = "The ticket ID to assign.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "The user ID to assign the ticket to.")]
    public Input<Guid> AssigneeUserId { get; set; } = default!;

    [Output(Description = "The user ID the ticket was assigned to.")]
    public Output<Guid> AssignedTo { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = context.Get(TicketId);
        var assigneeUserId = context.Get(AssigneeUserId);

        var mediator = context.GetRequiredService<ISender>();
        await mediator.Send(new AssignTicketCommand(ticketId, assigneeUserId));

        context.Set(AssignedTo, assigneeUserId);
        await context.CompleteActivityAsync();
    }
}
