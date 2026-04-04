using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.HR.Application.IntegrationEvents;

/// <summary>İzin talebi onaylandığında yayınlanır → Notification bildirim gönderir.</summary>
public sealed record LeaveApprovedEvent(
    Guid LeaveRequestId,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    int TotalDays,
    Guid ApprovedByUserId) : IntegrationEvent;

/// <summary>İzin talebi reddedildiğinde yayınlanır.</summary>
public sealed record LeaveRejectedEvent(
    Guid LeaveRequestId,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveType,
    Guid RejectedByUserId,
    string? Reason) : IntegrationEvent;
