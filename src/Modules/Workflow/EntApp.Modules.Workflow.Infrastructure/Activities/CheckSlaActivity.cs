using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Queries;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Ticket'ın SLA durumunu kontrol eder.
/// Outcomes: OK, ResponseBreached, ResolutionBreached
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Checks the SLA status of a ticket and routes based on breach status.",
    DisplayName = "Check SLA")]
public sealed class CheckSlaActivity : CodeActivity
{
    [Input(Description = "The ticket ID to check SLA for.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Output(Description = "Whether the response SLA has been breached.")]
    public Output<bool> ResponseBreached { get; set; } = default!;

    [Output(Description = "Whether the resolution SLA has been breached.")]
    public Output<bool> ResolutionBreached { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);

        var mediator = context.GetRequiredService<ISender>();
        var ticket = await mediator.Send(new GetTicketQuery(ticketId));

        if (ticket is null)
        {
            await context.CompleteActivityWithOutcomesAsync("Error");
            return;
        }

        context.Set(ResponseBreached, ticket.SlaResponseBreached);
        context.Set(ResolutionBreached, ticket.SlaResolutionBreached);

        if (ticket.SlaResolutionBreached)
            await context.CompleteActivityWithOutcomesAsync("ResolutionBreached");
        else if (ticket.SlaResponseBreached)
            await context.CompleteActivityWithOutcomesAsync("ResponseBreached");
        else
            await context.CompleteActivityWithOutcomesAsync("OK");
    }
}
