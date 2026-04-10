using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Validators;

/// <summary>
/// Elsa publish-time validasyonu.
/// Workflow publish edilirken activity'leri tarar ve en az bir <c>RouteToQueueActivity</c>
/// bulunmasını zorunlu kılar. Ayrıca blocking activity'den önce RouteToQueue gelmesini kontrol eder.
/// </summary>
public sealed class WorkflowRouteValidator
    : INotificationHandler<WorkflowDefinitionValidating>
{
    private const string RouteToQueueType = "RouteToQueueActivity";

    /// <summary>Blocking activity tiplerinin listesi (bookmark oluşturan / kullanıcı etkileşimi gerektiren).</summary>
    private static readonly HashSet<string> BlockingActivityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "WaitForApprovalActivity",
    };

    private readonly ILogger<WorkflowRouteValidator> _logger;

    public WorkflowRouteValidator(ILogger<WorkflowRouteValidator> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(WorkflowDefinitionValidating notification, CancellationToken ct)
    {
        var workflow = notification.Workflow;
        var root = workflow.Root;

        if (root is null)
        {
            _logger.LogWarning("Workflow has no root activity — skipping RouteToQueue validation.");
            return Task.CompletedTask;
        }

        // Tüm activity'leri recursive olarak topla
        var allActivities = new List<IActivity>();
        CollectActivities(root, allActivities);

        var hasRouteToQueue = allActivities.Any(a =>
            a.Type.Contains(RouteToQueueType, StringComparison.OrdinalIgnoreCase));

        if (!hasRouteToQueue)
        {
            notification.ValidationErrors.Add(new WorkflowValidationError(
                "Workflow en az bir 'Route to Queue' activity içermelidir. " +
                "Her talep mutlaka bir kuyruğa yönlendirilmelidir.",
                root.Id));

            _logger.LogWarning("Workflow validation failed: No RouteToQueueActivity found.");
        }

        // Blocking activity'lerin varlığını kontrol et
        // Eğer blocking activity var ama RouteToQueue yoksa zaten yukarıda yakalandı.
        // Ekstra kontrol: blocking activity, liste sırasında RouteToQueue'dan önce mi?
        // (Basitleştirilmiş kontrol — Sequence tabanlı workflow'lar için)
        if (hasRouteToQueue)
        {
            var firstBlockingIndex = allActivities.FindIndex(a =>
                BlockingActivityTypes.Contains(a.Type));
            var firstRouteIndex = allActivities.FindIndex(a =>
                a.Type.Contains(RouteToQueueType, StringComparison.OrdinalIgnoreCase));

            if (firstBlockingIndex >= 0 && firstRouteIndex >= 0 && firstBlockingIndex < firstRouteIndex)
            {
                notification.ValidationErrors.Add(new WorkflowValidationError(
                    "'Route to Queue' activity, kullanıcı etkileşimi gerektiren adımlardan (ör: Wait for Approval) " +
                    "ÖNCE çalışmalıdır. Aksi halde talep hiçbir kuyruğa atanmadan beklemede kalır.",
                    allActivities[firstBlockingIndex].Id));

                _logger.LogWarning(
                    "Workflow validation failed: Blocking activity at index {BlockingIdx} " +
                    "appears before RouteToQueue at index {RouteIdx}.",
                    firstBlockingIndex, firstRouteIndex);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// IActivity'den başlayarak tüm child activity'leri recursive olarak toplar.
    /// Elsa v3'te container activity'ler (Sequence, Flowchart, If, Switch vb.)
    /// genellikle bir Activities veya benzeri property üzerinden çocuk activity'lere sahiptir.
    /// </summary>
    private static void CollectActivities(IActivity activity, List<IActivity> result)
    {
        result.Add(activity);

        // Reflection ile 'Activities' property'sini ara (Sequence, Flowchart vb.)
        var activitiesProp = activity.GetType().GetProperty("Activities");
        if (activitiesProp?.GetValue(activity) is IEnumerable<IActivity> children)
        {
            foreach (var child in children)
            {
                CollectActivities(child, result);
            }
        }

        // 'Then' property'si (If activity) 
        var thenProp = activity.GetType().GetProperty("Then");
        if (thenProp?.GetValue(activity) is IActivity thenChild)
        {
            CollectActivities(thenChild, result);
        }

        // 'Else' property'si (If activity)
        var elseProp = activity.GetType().GetProperty("Else");
        if (elseProp?.GetValue(activity) is IActivity elseChild)
        {
            CollectActivities(elseChild, result);
        }

        // 'Body' property'si (ForEach, While vb.)
        var bodyProp = activity.GetType().GetProperty("Body");
        if (bodyProp?.GetValue(activity) is IActivity bodyChild)
        {
            CollectActivities(bodyChild, result);
        }
    }
}
