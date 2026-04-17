using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Domain.Enums;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Blocking activity — workflow duraklar ve ticket birine atanana kadar bekler.
/// 
/// Ticket kuyrukta beklerken henüz kimseye atanmamışken status değiştirilememesi
/// için WaitForStatusDecision'dan önce kullanılır.
/// 
/// Akış: RouteToQueue → WaitForAssignment → WaitForStatusDecision → Done
/// 
/// Resume mekanizması: Event-driven
///   - AssignTicketCommand veya ClaimTicketCommand çalıştığında TicketAssignedEvent publish edilir
///   - WorkflowAssignmentHandler bu event'i dinler ve bookmark'ı resume eder
///   - Hem dropdown ile atama hem "Üstlen" butonu ile self-assign desteklenir
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Pauses the workflow until the ticket is assigned to someone. " +
    "Blocks status changes until assignment is complete.",
    DisplayName = "Wait for Assignment")]
public sealed class WaitForAssignmentActivity : Activity
{
    [Input(Description = "The ticket ID awaiting assignment.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Label shown on the assignment step (e.g. 'Atama Bekliyor').")]
    public Input<string> Label { get; set; } = default!;

    [Input(Description = "If true, ticket status is automatically set to InProgress upon assignment. Default: true.")]
    public Input<bool> AutoSetInProgress { get; set; } = new(true);

    [Output(Description = "The user ID the ticket was assigned to.")]
    public Output<Guid> AssignedUserId { get; set; } = default!;

    [Output(Description = "The display name of the assigned user.")]
    public Output<string?> AssignedUserName { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);
        var label = context.Get(Label) ?? "Atama Bekliyor";
        var autoSetInProgress = context.Get(AutoSetInProgress);

        // Activity property'lerini sakla — resume'da tekrar okunabilir
        context.SetProperty("ResolvedTicketId", ticketId.ToString());
        context.SetProperty("ResolvedLabel", label);
        context.SetProperty("ResolvedAutoSetInProgress", autoSetInProgress.ToString());

        // Bookmark oluştur — workflow burada duraklar
        // WorkflowAssignmentHandler (TicketAssignedEvent dinleyici) bu bookmark'ı resume edecek
        //
        // ÖNEMLI: Bookmark payload SADECE TicketId içerir — Elsa hash eşleşmesi için.
        // Label ve AutoSetInProgress activity property olarak saklanır.
        // Bu, WorkflowAssignmentHandler'ın sadece TicketId bilerek resume yapabilmesini sağlar.
        var payload = new AssignmentBookmarkPayload(ticketId);
        context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: false);
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnResumed(ActivityExecutionContext context)
    {
        var input = context.WorkflowInput;

        // Atanan kullanıcı bilgileri (WorkflowAssignmentHandler veya endpoint tarafından gönderilir)
        var assigneeUserId = Guid.Empty;
        if (input.TryGetValue("AssigneeUserId", out var uid))
        {
            if (uid is Guid g) assigneeUserId = g;
            else if (uid is string s && Guid.TryParse(s, out var parsed)) assigneeUserId = parsed;
        }

        // ── Guard: AssigneeUserId yoksa workflow ilerletme ──────────────
        // Kullanıcı aksiyonlar panelinden "Assign" seçip tıklamış olabilir ama
        // kimi atayacağını belirtmemiş. Bu durumda bookmark'ı tekrar oluştur,
        // workflow duraklamaya devam etsin.
        if (assigneeUserId == Guid.Empty)
        {
            var ticketId = ResolveStoredTicketId(context);
            var payload = new AssignmentBookmarkPayload(ticketId);
            context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: true);
            return; // Workflow tekrar duraklar — activity tamamlanmaz
        }

        var assigneeUserName = input.TryGetValue("AssigneeUserName", out var uname)
            ? uname?.ToString()
            : null;

        // TicketId'yi çöz
        var resolvedTicketId = ResolveStoredTicketId(context);

        if (resolvedTicketId != Guid.Empty)
        {
            var mediator = context.GetRequiredService<ISender>();

            // 1. Ticket'ı atama yap (event-driven flow'da zaten atanmış olabilir,
            //    AssignTicketCommand idempotent olarak davranır)
            try
            {
                await mediator.Send(new AssignTicketCommand(resolvedTicketId, assigneeUserId));
            }
            catch
            {
                // Ticket zaten bu kişiye atanmışsa veya başka bir hata varsa devam et
                // Event-driven flow'da atama zaten yapılmış olur
            }

            // 2. AutoSetInProgress: atandığında status otomatik InProgress yapılsın mı
            var autoSetInProgress = true;
            var autoSetProp = context.GetProperty<string>("ResolvedAutoSetInProgress");
            if (!string.IsNullOrEmpty(autoSetProp) && bool.TryParse(autoSetProp, out var autoVal))
            {
                autoSetInProgress = autoVal;
            }

            if (autoSetInProgress)
            {
                await mediator.Send(new ChangeTicketStatusCommand(
                    resolvedTicketId, TicketStatus.InProgress, "Ticket atandı — otomatik InProgress"));
            }
        }

        // Output'ları set et
        context.Set(AssignedUserId, assigneeUserId);
        context.Set(AssignedUserName, assigneeUserName);

        // Tek outcome: Done → sıradaki adıma ilerle
        await context.CompleteActivityAsync();
    }

    /// <summary>
    /// Activity property'den veya workflow input'undan TicketId'yi çözer.
    /// </summary>
    private Guid ResolveStoredTicketId(ActivityExecutionContext context)
    {
        var storedId = context.GetProperty<string>("ResolvedTicketId");
        if (!string.IsNullOrEmpty(storedId) && Guid.TryParse(storedId, out var parsedTicketId))
            return parsedTicketId;

        return ActivityHelpers.ResolveTicketId(context, TicketId);
    }
}

/// <summary>
/// Bookmark payload — atama adımı için ticket eşleşmesi.
/// Elsa bu payload'ın hash'ini hesaplar; resume sırasında aynı payload verildiğinde eşleşir.
/// SADECE TicketId içerir — Label ve AutoSetInProgress activity property olarak saklanır.
/// Bu, WorkflowAssignmentHandler'ın sadece TicketId bilerek stimulus gönderebilmesini sağlar.
/// </summary>
public sealed record AssignmentBookmarkPayload(Guid TicketId);
