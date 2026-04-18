using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Blocking activity — workflow duraklar ve kullanıcıdan status kararı bekler.
/// 
/// Designer'da izin verilen status subset'i tanımlanır (ör: "Resolved, Cancelled, Escalated").
/// Frontend bu listeyi butonlar olarak gösterir.
/// Kullanıcı bir buton seçtiğinde:
///   1. Ticket status'ü otomatik olarak değişir (ChangeTicketStatusCommand)
///   2. Workflow "Done" outcome ile sıradaki adıma ilerler
///   3. ReturnToPool seçilirse → assignee kaldırılır, WaitForAssignment'a döner
/// 
/// Ayrı ChangeTicketStatus activity'si bağlamaya gerek yoktur.
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Pauses the workflow and waits for the user to select a ticket status. " +
    "Automatically changes the ticket status upon selection.",
    DisplayName = "Wait for Status Decision")]
[FlowNode("Done", "ReturnToPool")]
public sealed class WaitForStatusDecisionActivity : Activity
{
    [Input(Description = "The ticket ID awaiting a status decision.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Label shown on the decision step (e.g. 'Yönetici Kararı', 'Onay Adımı').")]
    public Input<string> Label { get; set; } = default!;

    [Input(Description = "Kullanıcıya sunulacak status seçenekleri (virgülle ayrılmış). " +
                          "Örnek: Resolved, Cancelled, Escalated. " +
                          "Geçerli değerler: New, Open, InProgress, WaitingForInfo, Escalated, Resolved, Closed, Cancelled, Reopened, ReturnToPool")]
    public Input<string> AllowedStatuses { get; set; } = default!;

    [Input(Description = "Optional: specific user ID who should decide. If empty, any queue member can decide.")]
    public Input<Guid?> DeciderUserId { get; set; } = default!;

    [Output(Description = "The status selected by the user.")]
    public Output<string> SelectedStatus { get; set; } = default!;

    [Output(Description = "Optional comment from the user.")]
    public Output<string?> Comment { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);
        var label = context.Get(Label) ?? "Durum Kararı";
        var deciderUserId = context.Get(DeciderUserId);

        // AllowedStatuses parse — ReturnToPool özel bir outcome, TicketStatus değil
        var rawStatuses = context.Get(AllowedStatuses) ?? "Resolved, Cancelled";
        var statuses = rawStatuses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => Enum.TryParse<TicketStatus>(s, true, out _) ||
                        string.Equals(s, "ReturnToPool", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (statuses.Length == 0)
            statuses = ["Resolved", "Cancelled"];

        // ReturnToPool her zaman seçenek olarak ekle (ticket status değil, workflow kontrol)
        if (!statuses.Contains("ReturnToPool", StringComparer.OrdinalIgnoreCase))
            statuses = [.. statuses, "ReturnToPool"];

        // Activity property'lerini sakla — resume'da tekrar okunabilir
        context.SetProperty("ResolvedTicketId", ticketId.ToString());
        context.SetProperty("ResolvedLabel", label);
        context.SetProperty("ResolvedAllowedStatuses", string.Join(", ", statuses));

        // Bookmark oluştur — workflow burada duraklar
        var payload = new StatusDecisionBookmarkPayload(ticketId, label, statuses, deciderUserId);
        context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: true);
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnResumed(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WaitForStatusDecisionActivity>>();
        var input = context.WorkflowInput;

        // Kullanıcının seçtiği status
        var selectedStatusStr = input.TryGetValue("Decision", out var d) ? d?.ToString() : null;
        var comment = input.TryGetValue("Comment", out var c) ? c?.ToString() : null;

        logger.LogInformation("[StatusDecision] OnResumed called. Decision={Decision}, Comment={Comment}", 
            selectedStatusStr, comment);

        // 1. Activity property'den oku (ExecuteAsync'te sakladık)
        var ticketId = Guid.Empty;
        var storedId = context.GetProperty<string>("ResolvedTicketId");
        if (!string.IsNullOrEmpty(storedId) && Guid.TryParse(storedId, out var parsed))
        {
            ticketId = parsed;
        }
        
        // 2. Fallback: ResolveTicketId (workflow input'undan)
        if (ticketId == Guid.Empty)
        {
            ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);
        }

        logger.LogInformation("[StatusDecision] TicketId={TicketId}", ticketId);

        if (ticketId == Guid.Empty)
        {
            logger.LogWarning("[StatusDecision] TicketId is empty! Completing with Done.");
            context.Set(SelectedStatus, selectedStatusStr);
            context.Set(Comment, comment);
            await context.CompleteActivityAsync();
            return;
        }

        // ── ReturnToPool: Havuza geri bırak ──
        // Flowchart routing güvenilir çalışmıyor — self-loop kullan:
        // 1. Ticket'ı unassign et
        // 2. Yeni AssignmentBookmarkPayload oluştur (workflow aynı activity'de duraklar)
        // 3. Frontend "Üzerime Al" gösterir
        if (string.Equals(selectedStatusStr, "ReturnToPool", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[StatusDecision] ReturnToPool branch entered for ticket {TicketId}", ticketId);
            
            try
            {
                var sender = context.GetRequiredService<ISender>();
                await sender.Send(new UnclaimTicketCommand(ticketId));
                logger.LogInformation("[StatusDecision] UnclaimTicketCommand succeeded for ticket {TicketId}", ticketId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[StatusDecision] UnclaimTicketCommand FAILED for ticket {TicketId}", ticketId);
            }

            // Yeni assignment bookmark oluştur — workflow burada tekrar duraklar
            var assignmentPayload = new AssignmentBookmarkPayload(ticketId);
            context.CreateBookmark(assignmentPayload, OnAssignmentResumed, includeActivityInstanceId: false);
            logger.LogInformation("[StatusDecision] AssignmentBookmark created — waiting for re-assignment");
            return; // Activity tamamlanMAZ
        }

        // ── Normal status değişikliği ──
        if (string.IsNullOrWhiteSpace(selectedStatusStr) ||
            !Enum.TryParse<TicketStatus>(selectedStatusStr, true, out var newStatus))
        {
            newStatus = TicketStatus.Resolved;
            selectedStatusStr = "Resolved";
        }

        // Status'ü otomatik değiştir
        var mediator = context.GetRequiredService<ISender>();
        var reason = comment ?? $"Workflow kararı: {selectedStatusStr}";
        await mediator.Send(new ChangeTicketStatusCommand(ticketId, newStatus, reason));

        // Output'ları set et
        context.Set(SelectedStatus, selectedStatusStr);
        context.Set(Comment, comment);

        // Tek outcome: Done → sıradaki adıma ilerle
        await context.CompleteActivityAsync();
    }

    /// <summary>
    /// ReturnToPool sonrası tekrar atama yapıldığında çağrılır.
    /// Ticket'ı atar, status → InProgress, StatusDecision bookmark yeniden oluşturur.
    /// </summary>
    private async ValueTask OnAssignmentResumed(ActivityExecutionContext context)
    {
        var logger = context.GetRequiredService<ILogger<WaitForStatusDecisionActivity>>();
        var input = context.WorkflowInput;

        logger.LogInformation("[StatusDecision] OnAssignmentResumed called");

        // Atanan kullanıcı ID
        var assigneeUserId = Guid.Empty;
        if (input.TryGetValue("AssigneeUserId", out var uid))
        {
            if (uid is Guid g) assigneeUserId = g;
            else if (uid is string s && Guid.TryParse(s, out var parsed)) assigneeUserId = parsed;
        }

        var ticketId = Guid.Parse(context.GetProperty<string>("ResolvedTicketId") ?? Guid.Empty.ToString());

        if (assigneeUserId != Guid.Empty && ticketId != Guid.Empty)
        {
            var mediator = context.GetRequiredService<ISender>();

            // 1. Ticket'ı ata
            try { await mediator.Send(new EntApp.Modules.RequestManagement.Application.Commands.ClaimTicketCommand(ticketId, assigneeUserId)); }
            catch (Exception ex) { logger.LogWarning(ex, "[StatusDecision] ClaimTicket failed"); }

            // 2. Status → InProgress
            try { await mediator.Send(new ChangeTicketStatusCommand(ticketId, TicketStatus.InProgress, "Ticket yeniden atandı")); }
            catch (Exception ex) { logger.LogWarning(ex, "[StatusDecision] ChangeStatus failed"); }
        }

        // 3. StatusDecision bookmark'ı yeniden oluştur
        var rawStatuses = context.GetProperty<string>("ResolvedAllowedStatuses") ?? "Resolved, Cancelled";
        var label = context.GetProperty<string>("ResolvedLabel") ?? "Durum Kararı";
        var statuses = rawStatuses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (!statuses.Contains("ReturnToPool", StringComparer.OrdinalIgnoreCase))
            statuses = [.. statuses, "ReturnToPool"];

        var payload = new StatusDecisionBookmarkPayload(ticketId, label, statuses, null);
        context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: true);
        logger.LogInformation("[StatusDecision] StatusDecision bookmark recreated — decision loop restarted");
    }
}

/// <summary>
/// Bookmark payload — status karar adımı bilgileri.
/// Frontend bu bilgiyi okuyarak dinamik butonlar gösterir.
/// </summary>
public sealed record StatusDecisionBookmarkPayload(
    Guid TicketId,
    string Label,
    string[] AllowedStatuses,
    Guid? DeciderUserId);
