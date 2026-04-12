using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Blocking activity — workflow duraklar ve tüm görevler tamamlanana kadar bekler.
/// AllSourceTasksCompletedEvent tetiklendiğinde WorkflowTaskCompletionHandler
/// bu aktivitenin bookmark'ını resume eder.
/// Elsa Designer'da "Ticket Management" kategorisinde görünür.
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

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var ticketId = context.Get(TicketId);

        // Bookmark oluştur — workflow burada duraklar
        // WorkflowTaskCompletionHandler bu bookmark'ı resume edecek
        var payload = new AllTasksDoneBookmarkPayload(ticketId);
        context.CreateBookmark(payload, OnResumed, includeActivityInstanceId: true);
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnResumed(ActivityExecutionContext context)
    {
        // Bookmark resume edildiğinde çalışır
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
