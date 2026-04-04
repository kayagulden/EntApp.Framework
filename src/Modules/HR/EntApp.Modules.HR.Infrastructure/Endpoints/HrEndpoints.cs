using EntApp.Modules.HR.Domain.Entities;
using EntApp.Modules.HR.Domain.Enums;
using EntApp.Modules.HR.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.HR.Infrastructure.Endpoints;

/// <summary>HR REST API endpoint'leri.</summary>
public static class HrEndpoints
{
    public static IEndpointRouteBuilder MapHrEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Employees ═══════════
        var emp = app.MapGroup("/api/hr/employees").WithTags("HR - Employees");

        emp.MapGet("/", async (HrDbContext db, string? search, string? department,
            string? status, int page = 1, int pageSize = 20) =>
        {
            var query = db.Employees.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(e => e.FirstName.Contains(search) || e.LastName.Contains(search)
                    || e.EmployeeNumber.Contains(search));
            if (!string.IsNullOrEmpty(department))
                query = query.Where(e => e.Department == department);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<EmployeeStatus>(status, out var s))
                query = query.Where(e => e.Status == s);

            var total = await query.CountAsync();
            var items = await query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(e => new { e.Id, e.EmployeeNumber, e.FirstName, e.LastName, e.Email,
                    e.Department, e.Position, Status = e.Status.ToString(),
                    EmploymentType = e.EmploymentType.ToString(), e.HireDate })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListEmployees");

        emp.MapGet("/{id:guid}", async (Guid id, HrDbContext db) =>
        {
            var e = await db.Employees.Include(x => x.DirectReports)
                .FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? Results.NotFound() : Results.Ok(e);
        }).WithName("GetEmployee");

        emp.MapPost("/", async (CreateEmployeeRequest req, HrDbContext db) =>
        {
            Enum.TryParse<EmploymentType>(req.EmploymentType, out var empType);
            var employee = EmployeeBase.Create(req.EmployeeNumber, req.FirstName, req.LastName,
                req.HireDate, empType, req.Email, req.Phone, req.NationalId,
                req.DateOfBirth, req.Department, req.Position, req.ManagerId,
                req.AnnualLeaveEntitlement);
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return Results.Created($"/api/hr/employees/{employee.Id}", new { employee.Id, employee.EmployeeNumber });
        }).WithName("CreateEmployee");

        // ── Org Chart ────────────────────────────────────
        emp.MapGet("/org-chart", async (HrDbContext db) =>
        {
            var employees = await db.Employees
                .Where(e => e.Status == EmployeeStatus.Active)
                .Select(e => new { e.Id, e.FirstName, e.LastName, e.Department,
                    e.Position, e.ManagerId })
                .ToListAsync();
            return Results.Ok(employees);
        }).WithName("OrgChart").WithSummary("Organizasyon şeması verisi");

        // ═══════════ Leave Requests ═══════════
        var leave = app.MapGroup("/api/hr/leave-requests").WithTags("HR - Leave Requests");

        leave.MapGet("/", async (HrDbContext db, Guid? employeeId, string? status,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.LeaveRequests.Include(l => l.Employee).AsQueryable();
            if (employeeId.HasValue) query = query.Where(l => l.EmployeeId == employeeId.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveRequestStatus>(status, out var s))
                query = query.Where(l => l.Status == s);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(l => new { l.Id, l.EmployeeId,
                    EmployeeName = l.Employee.FirstName + " " + l.Employee.LastName,
                    LeaveType = l.LeaveType.ToString(), Status = l.Status.ToString(),
                    l.StartDate, l.EndDate, l.TotalDays, l.Reason, l.CreatedAt })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListLeaveRequests");

        leave.MapPost("/", async (CreateLeaveRequest req, HrDbContext db) =>
        {
            Enum.TryParse<LeaveType>(req.LeaveType, out var type);
            var request = LeaveRequestBase.Create(req.EmployeeId, type,
                req.StartDate, req.EndDate, req.Reason);
            request.Submit();
            db.LeaveRequests.Add(request);
            await db.SaveChangesAsync();
            return Results.Created($"/api/hr/leave-requests/{request.Id}",
                new { request.Id, request.TotalDays, Status = request.Status.ToString() });
        }).WithName("CreateLeaveRequest");

        leave.MapPost("/{id:guid}/approve", async (Guid id, LeaveActionRequest req, HrDbContext db) =>
        {
            var lr = await db.LeaveRequests.FindAsync(id);
            if (lr is null) return Results.NotFound();
            lr.Approve(req.UserId, req.Comment);
            await db.SaveChangesAsync();
            return Results.Ok(new { lr.Id, Status = lr.Status.ToString() });
        }).WithName("ApproveLeave");

        leave.MapPost("/{id:guid}/reject", async (Guid id, LeaveActionRequest req, HrDbContext db) =>
        {
            var lr = await db.LeaveRequests.FindAsync(id);
            if (lr is null) return Results.NotFound();
            lr.Reject(req.UserId, req.Comment);
            await db.SaveChangesAsync();
            return Results.Ok(new { lr.Id, Status = lr.Status.ToString() });
        }).WithName("RejectLeave");

        // ── Leave Balance ────────────────────────────────
        leave.MapGet("/balance/{employeeId:guid}", async (Guid employeeId, HrDbContext db) =>
        {
            var employee = await db.Employees.FindAsync(employeeId);
            if (employee is null) return Results.NotFound();

            var usedDays = await db.LeaveRequests
                .Where(l => l.EmployeeId == employeeId && l.LeaveType == LeaveType.Annual
                    && l.Status == LeaveRequestStatus.Approved
                    && l.StartDate.Year == DateTime.UtcNow.Year)
                .SumAsync(l => l.TotalDays);

            return Results.Ok(new
            {
                employeeId,
                employeeName = employee.FullName,
                year = DateTime.UtcNow.Year,
                entitlement = employee.AnnualLeaveEntitlement,
                used = usedDays,
                remaining = employee.AnnualLeaveEntitlement - usedDays
            });
        }).WithName("LeaveBalance").WithSummary("Yıllık izin bakiyesi");

        // ═══════════ Attendance ═══════════
        var att = app.MapGroup("/api/hr/attendance").WithTags("HR - Attendance");

        att.MapGet("/", async (HrDbContext db, Guid? employeeId, DateTime? date,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Attendances.AsQueryable();
            if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
            if (date.HasValue) query = query.Where(a => a.Date == date.Value.Date);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.Date)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new { a.Id, a.EmployeeId, a.Date, a.CheckIn, a.CheckOut,
                    Status = a.Status.ToString(), a.WorkedHours, a.OvertimeHours })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListAttendances");

        att.MapPost("/", async (CreateAttendanceRequest req, HrDbContext db) =>
        {
            Enum.TryParse<AttendanceStatus>(req.Status, out var status);
            var attendance = AttendanceBase.Create(req.EmployeeId, req.Date,
                req.CheckIn, req.CheckOut, status, req.Notes);
            db.Attendances.Add(attendance);
            await db.SaveChangesAsync();
            return Results.Created($"/api/hr/attendance/{attendance.Id}",
                new { attendance.Id, attendance.WorkedHours, attendance.OvertimeHours });
        }).WithName("CreateAttendance");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
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
