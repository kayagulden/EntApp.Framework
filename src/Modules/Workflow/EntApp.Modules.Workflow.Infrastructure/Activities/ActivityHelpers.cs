using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace EntApp.Modules.Workflow.Infrastructure.Activities;

/// <summary>
/// Tüm ticket activity'leri tarafından paylaşılan yardımcı metotlar.
/// </summary>
public static class ActivityHelpers
{
    /// <summary>
    /// TicketId'yi çözer. Öncelik sırası:
    /// 1. Activity input property'si (Designer'da set edildiyse)
    /// 2. Workflow input'undan "TicketId" key'i (programatik başlatıldığında)
    /// 
    /// Bu fallback, Designer'da TicketId alanı boş bırakıldığında bile
    /// workflow'un doğru çalışmasını sağlar.
    /// </summary>
    public static Guid ResolveTicketId(ActivityExecutionContext context, Input<Guid> ticketIdInput)
    {
        // 1. Activity input'undan dene
        var ticketId = context.Get(ticketIdInput);
        if (ticketId != Guid.Empty)
            return ticketId;

        // 2. Workflow input'undan fallback
        var workflowInput = context.WorkflowExecutionContext.Input;
        if (workflowInput.TryGetValue("TicketId", out var inputVal))
        {
            if (inputVal is Guid g)
                return g;
            if (inputVal is string s && Guid.TryParse(s, out var parsed))
                return parsed;
        }

        return Guid.Empty;
    }
}
