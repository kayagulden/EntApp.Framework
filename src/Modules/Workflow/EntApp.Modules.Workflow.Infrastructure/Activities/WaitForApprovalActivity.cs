using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Blocking activity — workflow duraklar ve kullanıcı aksiyonu bekler.
/// Frontend'te bu activity'nin bookmark'ı sorgulanarak dinamik butonlar gösterilir.
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Pauses the workflow and waits for a user approval decision (Approve/Reject).",
    DisplayName = "Wait for Approval")]
public sealed class WaitForApprovalActivity : Activity
{
    [Input(Description = "The ticket ID awaiting approval.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Label shown on the approval step (e.g. 'Budget Approval', 'Installation Complete').")]
    public Input<string> ApprovalLabel { get; set; } = default!;

    [Input(Description = "Optional: specific user ID who should approve. If empty, any queue member can approve.")]
    public Input<Guid?> ApproverUserId { get; set; } = default!;

    [Input(Description = "Timeout in hours. 0 = no timeout.")]
    public Input<int> TimeoutHours { get; set; } = default!;

    [Output(Description = "The approval decision: 'Approved' or 'Rejected'.")]
    public Output<string> Decision { get; set; } = default!;

    [Output(Description = "Optional comment from the approver.")]
    public Output<string?> Comment { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = context.Get(TicketId);
        var label = context.Get(ApprovalLabel) ?? "Approval";
        var approverUserId = context.Get(ApproverUserId);

        // Bookmark oluştur — workflow burada duraklar
        // Frontend bu bookmark'ı sorgulayarak butonları gösterir
        var payload = new ApprovalBookmarkPayload(ticketId, label, approverUserId);
        context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: true);
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnResumed(ActivityExecutionContext context)
    {
        // Bookmark resume edildiğinde çalışır
        var input = context.WorkflowInput;

        var decision = input.TryGetValue("Decision", out var d) ? d?.ToString() ?? "Approved" : "Approved";
        var comment = input.TryGetValue("Comment", out var c) ? c?.ToString() : null;

        context.Set(Decision, decision);
        context.Set(Comment, comment);

        // Decision'a göre farklı outcome
        await context.CompleteActivityWithOutcomesAsync(decision);
    }
}

/// <summary>Bookmark payload — approval step kimlik bilgileri.</summary>
public sealed record ApprovalBookmarkPayload(
    Guid TicketId,
    string ApprovalLabel,
    Guid? ApproverUserId);
