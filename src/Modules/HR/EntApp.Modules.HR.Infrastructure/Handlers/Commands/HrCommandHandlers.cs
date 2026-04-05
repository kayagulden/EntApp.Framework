using EntApp.Modules.HR.Application.Commands;
using EntApp.Modules.HR.Application.IntegrationEvents;
using EntApp.Modules.HR.Domain.Entities;
using EntApp.Modules.HR.Domain.Enums;
using EntApp.Modules.HR.Domain.Ids;
using EntApp.Modules.HR.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.HR.Infrastructure.Handlers.Commands;

// ── Create Employee ─────────────────────────────────────────
public sealed class CreateEmployeeCommandHandler(HrDbContext db)
    : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        Enum.TryParse<EmploymentType>(request.EmploymentType, out var empType);
        var employee = EmployeeBase.Create(
            request.EmployeeNumber, request.FirstName, request.LastName,
            request.HireDate, empType, request.Email, request.Phone,
            request.NationalId, request.DateOfBirth, request.Department,
            request.Position,
            request.ManagerId.HasValue ? new EmployeeId(request.ManagerId.Value) : null,
            request.AnnualLeaveEntitlement);

        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return employee.Id.Value;
    }
}

// ── Create Leave Request ────────────────────────────────────
public sealed class CreateLeaveRequestCommandHandler(HrDbContext db)
    : IRequestHandler<CreateLeaveRequestCommand, CreateLeaveRequestResult>
{
    public async Task<CreateLeaveRequestResult> Handle(CreateLeaveRequestCommand request, CancellationToken ct)
    {
        Enum.TryParse<LeaveType>(request.LeaveType, out var type);
        var leaveRequest = LeaveRequestBase.Create(
            new EmployeeId(request.EmployeeId), type,
            request.StartDate, request.EndDate, request.Reason);
        leaveRequest.Submit();

        db.LeaveRequests.Add(leaveRequest);
        await db.SaveChangesAsync(ct);

        return new CreateLeaveRequestResult(
            leaveRequest.Id.Value, leaveRequest.TotalDays, leaveRequest.Status.ToString());
    }
}

// ── Approve Leave ───────────────────────────────────────────
public sealed class ApproveLeaveCommandHandler(HrDbContext db, IEventBus eventBus)
    : IRequestHandler<ApproveLeaveCommand, string>
{
    public async Task<string> Handle(ApproveLeaveCommand request, CancellationToken ct)
    {
        var lr = await db.LeaveRequests.Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id.Value == request.LeaveRequestId, ct)
            ?? throw new KeyNotFoundException($"Leave request {request.LeaveRequestId} not found");

        lr.Approve(request.UserId, request.Comment);
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new LeaveApprovedEvent(
            lr.Id.Value, lr.EmployeeId.Value, lr.Employee.FullName,
            lr.LeaveType.ToString(), lr.StartDate, lr.EndDate,
            lr.TotalDays, request.UserId));

        return lr.Status.ToString();
    }
}

// ── Reject Leave ────────────────────────────────────────────
public sealed class RejectLeaveCommandHandler(HrDbContext db, IEventBus eventBus)
    : IRequestHandler<RejectLeaveCommand, string>
{
    public async Task<string> Handle(RejectLeaveCommand request, CancellationToken ct)
    {
        var lr = await db.LeaveRequests.Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id.Value == request.LeaveRequestId, ct)
            ?? throw new KeyNotFoundException($"Leave request {request.LeaveRequestId} not found");

        lr.Reject(request.UserId, request.Comment);
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new LeaveRejectedEvent(
            lr.Id.Value, lr.EmployeeId.Value, lr.Employee.FullName,
            lr.LeaveType.ToString(), request.UserId, request.Comment));

        return lr.Status.ToString();
    }
}

// ── Create Attendance ───────────────────────────────────────
public sealed class CreateAttendanceCommandHandler(HrDbContext db)
    : IRequestHandler<CreateAttendanceCommand, CreateAttendanceResult>
{
    public async Task<CreateAttendanceResult> Handle(CreateAttendanceCommand request, CancellationToken ct)
    {
        Enum.TryParse<AttendanceStatus>(request.Status, out var status);
        var attendance = AttendanceBase.Create(
            new EmployeeId(request.EmployeeId), request.Date,
            request.CheckIn, request.CheckOut, status, request.Notes);

        db.Attendances.Add(attendance);
        await db.SaveChangesAsync(ct);

        return new CreateAttendanceResult(
            attendance.Id.Value, attendance.WorkedHours, attendance.OvertimeHours);
    }
}
