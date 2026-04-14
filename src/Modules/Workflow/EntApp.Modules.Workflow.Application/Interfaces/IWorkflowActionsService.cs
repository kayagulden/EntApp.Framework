namespace EntApp.Modules.Workflow.Application.Interfaces;

/// <summary>
/// Workflow instance'larındaki aktif bookmark'ları sorgulama ve resume etme servisi.
/// Genel amaçlıdır — Ticket, SalesOrder, LeaveRequest vb. her türlü domain entity için kullanılabilir.
/// </summary>
public interface IWorkflowActionsService
{
    /// <summary>Verilen workflow instance için kullanıcıya gösterilebilecek aksiyonları döner.</summary>
    Task<List<WorkflowActionDto>> GetAvailableActionsAsync(string workflowInstanceId, CancellationToken ct = default);

    /// <summary>Belirtilen bookmark'ı resume ederek workflow'u ilerletir.</summary>
    Task ResumeActionAsync(string workflowInstanceId, string bookmarkId, Dictionary<string, object>? input = null, CancellationToken ct = default);
}

/// <summary>Frontend'e dönen aksiyon bilgisi — her biri bir buton setine karşılık gelir.</summary>
public sealed record WorkflowActionDto(
    /// <summary>Elsa bookmark ID — resume ederken kullanılır.</summary>
    string BookmarkId,
    /// <summary>Activity tipi (ör: "WaitForApprovalActivity").</summary>
    string ActivityType,
    /// <summary>Kullanıcıya gösterilecek başlık (ör: "Bütçe Onayı").</summary>
    string Label,
    /// <summary>Olası çıktılar — her biri bir buton olur (ör: ["Approved", "Rejected"]).</summary>
    string[] Outcomes);
