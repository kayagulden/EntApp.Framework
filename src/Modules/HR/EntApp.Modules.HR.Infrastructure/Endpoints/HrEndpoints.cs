using EntApp.Modules.HR.Application.Commands;
using EntApp.Modules.HR.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.HR.Infrastructure.Endpoints;

/// <summary>HR REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class HrEndpoints
{
    public static IEndpointRouteBuilder MapHrEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Employees ═══════════
        var emp = app.MapGroup("/api/hr/employees").WithTags("HR - Employees");

        emp.MapGet("/", async (ISender mediator, string? search, string? department,
            string? status, int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListEmployeesQuery(search, department, status, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListEmployees");

        emp.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetEmployeeQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetEmployee");

        emp.MapPost("/", async (CreateEmployeeRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateEmployeeCommand(
                req.EmployeeNumber, req.FirstName, req.LastName, req.HireDate,
                req.EmploymentType, req.Email, req.Phone, req.NationalId,
                req.DateOfBirth, req.Department, req.Position,
                req.ManagerId, req.AnnualLeaveEntitlement));
            return Results.Created($"/api/hr/employees/{id}", new { id });
        }).WithName("CreateEmployee");

        emp.MapGet("/org-chart", async (ISender mediator) =>
        {
            var result = await mediator.Send(new GetOrgChartQuery());
            return Results.Ok(result);
        }).WithName("OrgChart").WithSummary("Organizasyon şeması verisi");

        // ═══════════ Leave Requests ═══════════
        var leave = app.MapGroup("/api/hr/leave-requests").WithTags("HR - Leave Requests");

        leave.MapGet("/", async (ISender mediator, Guid? employeeId, string? status,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListLeaveRequestsQuery(employeeId, status, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListLeaveRequests");

        leave.MapPost("/", async (CreateLeaveRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateLeaveRequestCommand(
                req.EmployeeId, req.LeaveType, req.StartDate, req.EndDate, req.Reason));
            return Results.Created($"/api/hr/leave-requests/{result.Id}", result);
        }).WithName("CreateLeaveRequest");

        leave.MapPost("/{id:guid}/approve", async (Guid id, LeaveActionRequest req, ISender mediator) =>
        {
            var status = await mediator.Send(new ApproveLeaveCommand(id, req.UserId, req.Comment));
            return Results.Ok(new { id, status });
        }).WithName("ApproveLeave");

        leave.MapPost("/{id:guid}/reject", async (Guid id, LeaveActionRequest req, ISender mediator) =>
        {
            var status = await mediator.Send(new RejectLeaveCommand(id, req.UserId, req.Comment));
            return Results.Ok(new { id, status });
        }).WithName("RejectLeave");

        leave.MapGet("/balance/{employeeId:guid}", async (Guid employeeId, ISender mediator) =>
        {
            var result = await mediator.Send(new GetLeaveBalanceQuery(employeeId));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("LeaveBalance").WithSummary("Yıllık izin bakiyesi");

        // ═══════════ Attendance ═══════════
        var att = app.MapGroup("/api/hr/attendance").WithTags("HR - Attendance");

        att.MapGet("/", async (ISender mediator, Guid? employeeId, DateTime? date,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListAttendancesQuery(employeeId, date, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListAttendances");

        att.MapPost("/", async (CreateAttendanceRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateAttendanceCommand(
                req.EmployeeId, req.Date, req.CheckIn, req.CheckOut, req.Status, req.Notes));
            return Results.Created($"/api/hr/attendance/{result.Id}", result);
        }).WithName("CreateAttendance");

        return app;
    }
}

// ── Request DTO'lar (HTTP body) ─────────────────────────────
public sealed record CreateEmployeeRequest(string EmployeeNumber, string FirstName, string LastName,
    DateTime HireDate, string EmploymentType = "FullTime", string? Email = null, string? Phone = null,
    string? NationalId = null, DateTime? DateOfBirth = null, string? Department = null,
    string? Position = null, Guid? ManagerId = null, int AnnualLeaveEntitlement = 14);

public sealed record CreateLeaveRequest(Guid EmployeeId, string LeaveType,
    DateTime StartDate, DateTime EndDate, string? Reason = null);

public sealed record LeaveActionRequest(Guid UserId, string? Comment = null);

public sealed record CreateAttendanceRequest(Guid EmployeeId, DateTime Date,
    TimeSpan? CheckIn = null, TimeSpan? CheckOut = null,
    string Status = "Present", string? Notes = null);
