using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.TaskManagement.Application.IntegrationEvents;

/// <summary>Bir kaynağa (Ticket vb.) görev oluşturulduğunda.</summary>
public sealed record TaskCreatedForSourceEvent(
    Guid TaskId, string TaskNumber,
    string SourceModule, string SourceType, Guid SourceId,
    Guid? AssigneeUserId) : IntegrationEvent;

/// <summary>Bir görevin durumu değiştiğinde.</summary>
public sealed record TaskStatusChangedEvent(
    Guid TaskId, string TaskNumber,
    string OldStatus, string NewStatus,
    string? SourceModule, string? SourceType, Guid? SourceId
) : IntegrationEvent;

/// <summary>Bir kaynağa bağlı TÜM görevler tamamlandığında (Done veya Cancelled).</summary>
public sealed record AllSourceTasksCompletedEvent(
    string SourceModule, string SourceType, Guid SourceId,
    int CompletedTaskCount) : IntegrationEvent;
