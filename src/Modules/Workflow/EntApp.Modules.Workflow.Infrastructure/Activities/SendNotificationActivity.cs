using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Bildirim gönderir. Faz 1'de stub — sadece loglar.
/// Faz 2'de Notification modülüne integration event dispatch edilecek.
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Sends a notification to a user about a ticket action.",
    DisplayName = "Send Notification")]
public sealed class SendNotificationActivity : CodeActivity
{
    [Input(Description = "The ticket ID the notification is about.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Recipient user ID.")]
    public Input<Guid> RecipientUserId { get; set; } = default!;

    [Input(Description = "Notification template/message.")]
    public Input<string> Template { get; set; } = default!;

    [Output(Description = "Generated notification ID (stub in Phase 1).")]
    public Output<Guid> NotificationId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = context.Get(TicketId);
        var recipientUserId = context.Get(RecipientUserId);
        var template = context.Get(Template);

        var logger = context.GetRequiredService<ILogger<SendNotificationActivity>>();
        var notificationId = Guid.NewGuid();

        // Faz 1: Stub — sadece log
        // TODO: Faz 2'de Notification modülüne integration event gönder
        logger.LogInformation(
            "[Elsa] SendNotification — Ticket: {TicketId}, Recipient: {RecipientUserId}, Template: {Template}, NotificationId: {NotificationId}",
            ticketId, recipientUserId, template, notificationId);

        context.Set(NotificationId, notificationId);
        await context.CompleteActivityAsync();
    }
}
