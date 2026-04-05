using MediatR;

namespace EntApp.Modules.HR.Application.Commands;

// ── Create Employee ─────────────────────────────────────────
public sealed record CreateEmployeeCommand(
    string EmployeeNumber, string FirstName, string LastName,
    DateTime HireDate, string EmploymentType = "FullTime",
    string? Email = null, string? Phone = null,
    string? NationalId = null, DateTime? DateOfBirth = null,
    string? Department = null, string? Position = null,
    Guid? ManagerId = null, int AnnualLeaveEntitlement = 14) : IRequest<Guid>;

// ── Create Leave Request ────────────────────────────────────
public sealed record CreateLeaveRequestCommand(
    Guid EmployeeId, string LeaveType,
    DateTime StartDate, DateTime EndDate,
    string? Reason = null) : IRequest<CreateLeaveRequestResult>;

public sealed record CreateLeaveRequestResult(Guid Id, int TotalDays, string Status);

// ── Approve Leave ───────────────────────────────────────────
public sealed record ApproveLeaveCommand(
    Guid LeaveRequestId, Guid UserId, string? Comment = null) : IRequest<string>;

// ── Reject Leave ────────────────────────────────────────────
public sealed record RejectLeaveCommand(
    Guid LeaveRequestId, Guid UserId, string? Comment = null) : IRequest<string>;

// ── Create Attendance ───────────────────────────────────────
public sealed record CreateAttendanceCommand(
    Guid EmployeeId, DateTime Date,
    TimeSpan? CheckIn = null, TimeSpan? CheckOut = null,
    string Status = "Present", string? Notes = null) : IRequest<CreateAttendanceResult>;

public sealed record CreateAttendanceResult(Guid Id, decimal WorkedHours, decimal OvertimeHours);
