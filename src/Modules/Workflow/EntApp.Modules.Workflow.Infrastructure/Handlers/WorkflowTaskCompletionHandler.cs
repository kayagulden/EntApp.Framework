using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Stimuli;
using EntApp.Modules.TaskManagement.Application.IntegrationEvents;
using EntApp.Modules.Workflow.Infrastructure.Activities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Handlers;

/// <summary>
/// Cross-module event handler: TaskManagement → Workflow (Elsa).
/// AllSourceTasksCompletedEvent geldiğinde, ilgili ticket'ın
/// WaitForAllTasksDoneActivity bookmark'ını resume eder.
/// Bu sayede workflow otomatik olarak bir sonraki adıma geçer.
/// </summary>
public sealed class WorkflowTaskCompletionHandler(
    IStimulusSender stimulusSender,
    ILogger<WorkflowTaskCompletionHandler> logger)
    : INotificationHandler<AllSourceTasksCompletedEvent>
{
    public async Task Handle(AllSourceTasksCompletedEvent notification, CancellationToken ct)
    {
        // Sadece RequestManagement.Ticket kaynağından gelen event'leri işle
        if (notification.SourceModule != "RequestManagement" || notification.SourceType != "Ticket")
            return;

        logger.LogInformation(
            "WorkflowTaskCompletionHandler: Ticket {SourceId} tüm görevler tamamlandı ({Count}). " +
            "Elsa bookmark resume ediliyor...",
            notification.SourceId, notification.CompletedTaskCount);

        try
        {
            // WaitForAllTasksDoneActivity'nin bookmark payload'ı ile eşleşen stimulus oluştur
            var payload = new AllTasksDoneBookmarkPayload(notification.SourceId);

            var metadata = new StimulusMetadata
            {
                Input = new Dictionary<string, object>
                {
                    ["CompletedTaskCount"] = notification.CompletedTaskCount
                }
            };

            // Elsa'ya stimulus gönder — matching bookmark varsa workflow resume olur
            await stimulusSender.SendAsync(
                nameof(WaitForAllTasksDoneActivity),
                payload,
                metadata,
                ct);

            logger.LogInformation(
                "WorkflowTaskCompletionHandler: Ticket {SourceId} için Elsa stimulus gönderildi.",
                notification.SourceId);
        }
        catch (Exception ex)
        {
            // Workflow bulunamazsa veya bookmark yoksa hata loglayıp yut
            // (event handler diğer handler'ları engellemeli)
            logger.LogWarning(ex,
                "WorkflowTaskCompletionHandler: Ticket {SourceId} için Elsa bookmark resume başarısız. " +
                "Aktif workflow olmayabilir.",
                notification.SourceId);
        }
    }
}
