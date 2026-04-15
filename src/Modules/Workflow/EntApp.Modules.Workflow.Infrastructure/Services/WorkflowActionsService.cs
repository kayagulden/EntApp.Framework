using System.Text.Json;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Requests;
using EntApp.Modules.Workflow.Application.Interfaces;
using EntApp.Modules.Workflow.Infrastructure.Activities;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Services;

/// <summary>
/// Elsa v3 bookmark store üzerinden aktif aksiyonları sorgular ve resume eder.
/// Genel amaçlıdır — her türlü blocking activity bookmark'ını destekler.
/// Outcome listesini bookmark payload'ından dinamik olarak okur.
/// </summary>
public sealed class WorkflowActionsService(
    IBookmarkStore bookmarkStore,
    IWorkflowDispatcher workflowDispatcher,
    ILogger<WorkflowActionsService> logger) : IWorkflowActionsService
{
    /// <summary>Fallback outcome listesi — payload'da PossibleOutcomes bulunamazsa.</summary>
    private static readonly Dictionary<string, string[]> FallbackOutcomes = new()
    {
        ["WaitForApprovalActivity"] = ["Approved", "Rejected"],
        ["EntApp.WaitForApprovalActivity"] = ["Approved", "Rejected"],
        ["WaitForStatusDecisionActivity"] = ["Resolved", "Cancelled"],
        ["WaitForAssignmentActivity"] = ["ClaimSelf"],
        ["EntApp.WaitForAssignmentActivity"] = ["ClaimSelf"],
    };

    public async Task<List<WorkflowActionDto>> GetAvailableActionsAsync(
        string workflowInstanceId, CancellationToken ct = default)
    {
        var filter = new BookmarkFilter
        {
            WorkflowInstanceId = workflowInstanceId
        };

        IEnumerable<StoredBookmark> bookmarks;
        try
        {
            bookmarks = await bookmarkStore.FindManyAsync(filter, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bookmark store sorgulanırken hata oluştu (InstanceId: {InstanceId})", workflowInstanceId);
            return [];
        }

        var actions = new List<WorkflowActionDto>();

        foreach (var bookmark in bookmarks)
        {
            var activityType = bookmark.Name ?? "Unknown";
            var label = ExtractLabel(bookmark);
            var outcomes = ExtractOutcomes(bookmark, activityType);
            var ticketId = ExtractTicketId(bookmark);

            actions.Add(new WorkflowActionDto(
                BookmarkId: bookmark.Id,
                ActivityType: activityType,
                Label: label,
                Outcomes: outcomes,
                TicketId: ticketId));
        }

        logger.LogDebug(
            "WorkflowInstance {InstanceId} için {Count} aktif aksiyon bulundu",
            workflowInstanceId, actions.Count);

        return actions;
    }

    public async Task ResumeActionAsync(
        string workflowInstanceId, string bookmarkId,
        Dictionary<string, object>? input = null, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Bookmark resume ediliyor (InstanceId: {InstanceId}, BookmarkId: {BookmarkId})",
            workflowInstanceId, bookmarkId);

        // Elsa v3 IWorkflowDispatcher — asenkron, queue-based resume
        // DispatchWorkflowInstanceRequest + DispatchWorkflowOptions overload (3 param)
        // kullanılmalı — 2 param kullanıldığında DefinitionRequest overload'una gider!
        var request = new DispatchWorkflowInstanceRequest(workflowInstanceId)
        {
            BookmarkId = bookmarkId,
            Input = input ?? new Dictionary<string, object>()
        };

        await workflowDispatcher.DispatchAsync(request, null, ct);

        logger.LogInformation(
            "Bookmark resume dispatch tamamlandı (InstanceId: {InstanceId}, BookmarkId: {BookmarkId})",
            workflowInstanceId, bookmarkId);
    }

    /// <summary>
    /// Bookmark payload'ından outcome listesini çıkarır.
    /// Elsa payload'ı JsonElement veya deserialized typed obje olabilir.
    /// </summary>
    private static string[] ExtractOutcomes(StoredBookmark bookmark, string activityType)
    {
        try
        {
            switch (bookmark.Payload)
            {
                // Typed payload — doğrudan deserialized obje
                case StatusDecisionBookmarkPayload sdp when sdp.AllowedStatuses.Length > 0:
                    return sdp.AllowedStatuses;
                case ApprovalBookmarkPayload abp when abp.PossibleOutcomes.Length > 0:
                    return abp.PossibleOutcomes;
                case AssignmentBookmarkPayload:
                    return ["ClaimSelf"];

                // JsonElement — DB'den raw okuma
                case JsonElement { ValueKind: JsonValueKind.Object } payload:
                    if (TryGetStringArray(payload, "allowedStatuses", out var statuses))
                        return statuses;
                    if (TryGetStringArray(payload, "possibleOutcomes", out var outcomes))
                        return outcomes;
                    break;
            }
        }
        catch
        {
            // Payload parse edilemezse fallback kullan
        }

        return FallbackOutcomes.TryGetValue(activityType, out var known)
            ? known
            : ["Done"];
    }

    /// <summary>Bookmark payload'ından label çıkarır.</summary>
    private static string ExtractLabel(StoredBookmark bookmark)
    {
        try
        {
            switch (bookmark.Payload)
            {
                case StatusDecisionBookmarkPayload sdp:
                    return sdp.Label;
                case ApprovalBookmarkPayload abp:
                    return abp.ApprovalLabel;
                case AssignmentBookmarkPayload:
                    return "Atama Bekleniyor";

                case JsonElement { ValueKind: JsonValueKind.Object } payload:
                    if (payload.TryGetProperty("label", out var l1))
                        return l1.GetString() ?? "Durum Kararı Bekleniyor";
                    if (payload.TryGetProperty("approvalLabel", out var l2))
                        return l2.GetString() ?? "Onay Bekleniyor";
                    break;
            }
        }
        catch
        {
            // Payload parse edilemezse varsayılan label kullan
        }

        return bookmark.Name switch
        {
            "EntApp.WaitForStatusDecisionActivity" => "Durum Kararı Bekleniyor",
            "EntApp.WaitForApprovalActivity" => "Onay Bekleniyor",
            "WaitForAllTasksDoneActivity" => "Görevler Bekleniyor",
            "WaitForAssignmentActivity" or "EntApp.WaitForAssignmentActivity" => "Atama Bekleniyor",
            _ => "Aksiyon Bekleniyor"
        };
    }

    private static bool TryGetStringArray(JsonElement payload, string propertyName, out string[] result)
    {
        result = [];
        if (!payload.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return false;

        result = prop.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToArray();
        return result.Length > 0;
    }

    /// <summary>Bookmark payload'ından TicketId çıkarır (varsa).</summary>
    private static Guid? ExtractTicketId(StoredBookmark bookmark)
    {
        try
        {
            switch (bookmark.Payload)
            {
                case AssignmentBookmarkPayload abp:
                    return abp.TicketId;
                case StatusDecisionBookmarkPayload sdp:
                    return sdp.TicketId;
                case AllTasksDoneBookmarkPayload atdp:
                    return atdp.TicketId;

                case JsonElement { ValueKind: JsonValueKind.Object } payload:
                    if (payload.TryGetProperty("ticketId", out var tid) &&
                        tid.TryGetGuid(out var parsed))
                        return parsed;
                    break;
            }
        }
        catch
        {
            // Payload parse edilemezse null dön
        }

        return null;
    }
}
