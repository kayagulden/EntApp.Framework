using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using EntApp.Modules.TaskManagement.Application.Queries;
using MediatR;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Blocking activity — workflow duraklar ve tüm görevler tamamlanana kadar bekler.
/// AllSourceTasksCompletedEvent tetiklendiğinde WorkflowTaskCompletionHandler
/// bu aktivitenin bookmark'ını resume eder.
/// Elsa Designer'da "Ticket Management" kategorisinde görünür.
///
/// Edge cases:
/// - Hiç görev yoksa → "NoTasks" outcome ile hemen tamamlar (beklemez)
/// - Tüm görevler zaten tamamlanmışsa → "AllDone" outcome ile hemen tamamlar
/// - Bekleyen görevler varsa → bookmark oluşturur ve duraklar
/// </summary>
[Activity("EntApp", "Ticket Management",
    "Pauses the workflow until all tasks linked to the ticket are completed (Done or Cancelled).",
    DisplayName = "Wait for All Tasks Done")]
public sealed class WaitForAllTasksDoneActivity : Activity
{
    [Input(Description = "The ticket ID whose linked tasks to wait for.")]
    public Input<Guid> TicketId { get; set; } = default!;

    [Input(Description = "Timeout in hours. 0 = no timeout (wait indefinitely).")]
    public Input<int> TimeoutHours { get; set; } = default!;

    [Output(Description = "Number of completed tasks when all tasks are done.")]
    public Output<int> CompletedTaskCount { get; set; } = default!;

    private static readonly HashSet<string> DoneStatuses = ["Done", "Cancelled"];

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = ActivityHelpers.ResolveTicketId(context, TicketId);

        // Mevcut görev durumunu kontrol et
        var mediator = context.GetRequiredService<ISender>();
        var tasks = await mediator.Send(
            new ListTasksBySourceQuery("RequestManagement", "Ticket", ticketId));

        if (tasks.Count == 0)
        {
            // Hiç görev yoksa beklemeye gerek yok
            context.Set(CompletedTaskCount, 0);
            await context.CompleteActivityWithOutcomesAsync("NoTasks");
            return;
        }

        var allDone = tasks.All(t => DoneStatuses.Contains(t.Status));
        if (allDone)
        {
            // Tüm görevler zaten tamamlanmış
            context.Set(CompletedTaskCount, tasks.Count);
            await context.CompleteActivityWithOutcomesAsync("AllDone");
            return;
        }

        // Bekleyen görevler var — bookmark oluştur, workflow duraklar
        // WorkflowTaskCompletionHandler bu bookmark'ı resume edecek
        var payload = new AllTasksDoneBookmarkPayload(ticketId);
        context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: true);
    }

    private async ValueTask OnResumed(ActivityExecutionContext context)
    {
        // Bookmark resume edildiğinde çalışır (tüm görevler tamamlandığında)
        var input = context.WorkflowInput;

        var completedCount = input.TryGetValue("CompletedTaskCount", out var c)
            ? Convert.ToInt32(c)
            : 0;

        context.Set(CompletedTaskCount, completedCount);

        // Aktiviteyi "AllDone" outcome ile tamamla
        await context.CompleteActivityWithOutcomesAsync("AllDone");
    }
}

/// <summary>
/// Bookmark payload — ticket eşleşmesi için kullanılır.
/// Elsa bu payload'ın hash'ini hesaplar; resume sırasında aynı payload verildiğinde eşleşir.
/// </summary>
public sealed record AllTasksDoneBookmarkPayload(Guid TicketId);
