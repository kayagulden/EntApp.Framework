using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Stimuli;
using EntApp.Modules.RequestManagement.Application.IntegrationEvents;
using EntApp.Modules.Workflow.Infrastructure.Activities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Handlers;

/// <summary>
/// Cross-module event handler: RequestManagement → Workflow (Elsa).
/// TicketAssignedEvent geldiğinde, ilgili ticket'ın
/// WaitForAssignmentActivity bookmark'ını resume eder.
/// Bu sayede workflow otomatik olarak bir sonraki adıma geçer.
/// 
/// Hem AssignTicketCommand hem ClaimTicketCommand sonucu tetiklenen
/// TicketAssignedEvent'i dinler — dropdown ile atama veya "Üstlen" butonu
/// ile self-assign yapıldığında workflow ilerler.
/// 
/// Not: IStimulusSender Elsa tarafından scoped olarak kaydedildiğinden,
/// constructor injection yerine IServiceProvider ile yeni scope oluşturulur.
/// </summary>
public sealed class WorkflowAssignmentHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<WorkflowAssignmentHandler> logger)
    : INotificationHandler<TicketAssignedEvent>
{
    public async Task Handle(TicketAssignedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "WorkflowAssignmentHandler: Ticket {TicketId} ({TicketNumber}) atandı → UserId: {AssigneeUserId}. " +
            "Elsa bookmark resume ediliyor...",
            notification.TicketId, notification.TicketNumber, notification.AssigneeUserId);

        try
        {
            // Elsa'nın scoped servislerini çözmek için yeni scope oluştur
            using var scope = scopeFactory.CreateScope();
            var stimulusSender = scope.ServiceProvider.GetRequiredService<IStimulusSender>();

            // WaitForAssignmentActivity'nin bookmark payload'ı ile eşleşen stimulus oluştur
            // AssignmentBookmarkPayload sadece TicketId içerir — hash eşleşmesi bu sayede sağlanır
            var payload = new AssignmentBookmarkPayload(notification.TicketId);

            var metadata = new StimulusMetadata
            {
                Input = new Dictionary<string, object>
                {
                    ["AssigneeUserId"] = notification.AssigneeUserId,
                    ["AssigneeUserName"] = notification.TicketNumber // Şimdilik ticket number, IAM entegrasyonunda displayName olacak
                }
            };

            // Elsa'ya stimulus gönder — matching bookmark varsa workflow resume olur
            await stimulusSender.SendAsync(
                nameof(WaitForAssignmentActivity),
                payload,
                metadata,
                ct);

            logger.LogInformation(
                "WorkflowAssignmentHandler: Ticket {TicketId} için Elsa stimulus gönderildi.",
                notification.TicketId);
        }
        catch (Exception ex)
        {
            // Workflow bulunamazsa veya bookmark yoksa hata loglayıp yut
            // (event handler diğer handler'ları engellememelidir)
            logger.LogWarning(ex,
                "WorkflowAssignmentHandler: Ticket {TicketId} için Elsa bookmark resume başarısız. " +
                "Aktif workflow olmayabilir.",
                notification.TicketId);
        }
    }
}

