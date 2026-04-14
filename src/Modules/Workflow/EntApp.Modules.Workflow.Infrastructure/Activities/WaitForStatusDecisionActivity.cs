using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Domain.Enums;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Blocking activity — workflow duraklar ve kullanıcıdan status kararı bekler.
/// 
/// Designer'da izin verilen status subset'i tanımlanır (ör: "Resolved, Cancelled, Escalated").
/// Frontend bu listeyi butonlar olarak gösterir.
/// Kullanıcı bir buton seçtiğinde:
///   1. Ticket status'ü otomatik olarak değişir (ChangeTicketStatusCommand)
///   2. Workflow "Done" outcome ile sıradaki adıma ilerler
/// 
/// Ayrı ChangeTicketStatus activity'si bağlamaya gerek yoktur.
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Pauses the workflow and waits for the user to select a ticket status. " +
    "Automatically changes the ticket status upon selection.",
    DisplayName = "Wait for Status Decision")]
public sealed class WaitForStatusDecisionActivity : Activity
{
    [Input(Description = "The ticket ID awaiting a status decision.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Label shown on the decision step (e.g. 'Yönetici Kararı', 'Onay Adımı').")]
    public Input<string> Label { get; set; } = default!;

    [Input(Description = "Kullanıcıya sunulacak status seçenekleri (virgülle ayrılmış). " +
                          "Örnek: Resolved, Cancelled, Escalated. " +
                          "Geçerli değerler: New, Open, InProgress, WaitingForInfo, Escalated, Resolved, Closed, Cancelled, Reopened",
           UIHint = "multi-line")]
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

        // AllowedStatuses parse
        var rawStatuses = context.Get(AllowedStatuses) ?? "Resolved, Cancelled";
        var statuses = rawStatuses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => Enum.TryParse<TicketStatus>(s, true, out _))
            .ToArray();

        if (statuses.Length == 0)
            statuses = ["Resolved", "Cancelled"];

        // Bookmark oluştur — workflow burada duraklar
        var payload = new StatusDecisionBookmarkPayload(ticketId, label, statuses, deciderUserId);
        context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: true);
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnResumed(ActivityExecutionContext context)
    {
        var input = context.WorkflowInput;

        // Kullanıcının seçtiği status
        var selectedStatusStr = input.TryGetValue("Decision", out var d) ? d?.ToString() : null;
        var comment = input.TryGetValue("Comment", out var c) ? c?.ToString() : null;

        if (string.IsNullOrWhiteSpace(selectedStatusStr) ||
            !Enum.TryParse<TicketStatus>(selectedStatusStr, true, out var newStatus))
        {
            // Geçersiz seçim — varsayılan olarak Resolved
            newStatus = TicketStatus.Resolved;
            selectedStatusStr = "Resolved";
        }

        // TicketId'yi tekrar çöz
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);

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
