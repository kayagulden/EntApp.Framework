using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Commands;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Ticket'ı belirtilen hizmet kuyruğuna yönlendirir.
/// Çok aşamalı akışlarda (Satınalma → IT Kurulum) queue geçişlerini sağlar.
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Routes a ticket to the specified service queue.",
    DisplayName = "Route to Queue")]
public sealed class RouteToQueueActivity : CodeActivity
{
    [Input(Description = "The ticket ID to route.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Target queue ID to route to.")]
    public Input<Guid> QueueId { get; set; } = default!;

    [Output(Description = "The queue ID the ticket was routed to.")]
    public Output<Guid> RoutedQueueId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);
        var queueId = context.Get(QueueId);

        var mediator = context.GetRequiredService<ISender>();
        await mediator.Send(new RouteTicketToQueueCommand(ticketId, queueId));

        context.Set(RoutedQueueId, queueId);
        await context.CompleteActivityAsync();
    }
}
