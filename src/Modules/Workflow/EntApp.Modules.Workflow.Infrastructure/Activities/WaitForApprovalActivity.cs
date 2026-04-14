using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Blocking activity — workflow duraklar ve kullanıcı aksiyonu bekler.
/// Frontend'te bu activity'nin bookmark'ı sorgulanarak dinamik butonlar gösterilir.
/// 
/// PossibleOutcomes input'u ile Designer'dan configurable outcome listesi tanımlanabilir.
/// Varsayılan: ["Approved", "Rejected"]
/// Örnek: ["Onayla", "Reddet", "Eskale Et", "Bilgi İste"]
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Pauses the workflow and waits for a user decision. Outcomes are configurable.",
    DisplayName = "Wait for Approval")]
public sealed class WaitForApprovalActivity : Activity
{
    /// <summary>Varsayılan outcome listesi.</summary>
    private static readonly string[] DefaultOutcomes = ["Approved", "Rejected"];

    [Input(Description = "The ticket ID awaiting approval.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Label shown on the approval step (e.g. 'Budget Approval', 'Installation Complete').")]
    public Input<string> ApprovalLabel { get; set; } = default!;

    [Input(Description = "Kullanıcıya sunulacak aksiyon seçenekleri. Her biri bir buton olarak gösterilir. " +
                          "Boş bırakılırsa varsayılan: Approved, Rejected. " +
                          "Örnek: Onayla, Reddet, Eskale Et")]
    public Input<string?> PossibleOutcomes { get; set; } = default!;

    [Input(Description = "Optional: specific user ID who should approve. If empty, any queue member can approve.")]
    public Input<Guid?> ApproverUserId { get; set; } = default!;

    [Input(Description = "Timeout in hours. 0 = no timeout.")]
    public Input<int> TimeoutHours { get; set; } = default!;

    [Output(Description = "The user's decision — matches one of the PossibleOutcomes.")]
    public Output<string> Decision { get; set; } = default!;

    [Output(Description = "Optional comment from the user.")]
    public Output<string?> Comment { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);
        var label = context.Get(ApprovalLabel) ?? "Approval";
        var approverUserId = context.Get(ApproverUserId);

        // PossibleOutcomes: virgülle ayrılmış string → dizi
        var outcomesRaw = context.Get(PossibleOutcomes);
        var outcomes = ParseOutcomes(outcomesRaw);

        // Bookmark oluştur — workflow burada duraklar
        // Frontend bu bookmark'ı sorgulayarak butonları gösterir
        var payload = new ApprovalBookmarkPayload(ticketId, label, outcomes, approverUserId);
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

    /// <summary>
    /// Virgülle ayrılmış string'i outcome dizisine çevirir.
    /// Boş veya null ise varsayılan outcome'ları döner.
    /// </summary>
    private static string[] ParseOutcomes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultOutcomes;

        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts : DefaultOutcomes;
    }
}

/// <summary>
/// Bookmark payload — approval step kimlik bilgileri ve olası aksiyonlar.
/// Frontend bu bilgiyi okuyarak dinamik butonlar gösterir.
/// </summary>
public sealed record ApprovalBookmarkPayload(
    Guid TicketId,
    string ApprovalLabel,
    string[] PossibleOutcomes,
    Guid? ApproverUserId);
