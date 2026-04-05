namespace EntApp.Modules.HR.Application.DTOs;

public sealed record EmployeeListDto(
    Guid Id, string EmployeeNumber, string FirstName, string LastName,
    string? Email, string? Department, string? Position,
    string Status, string EmploymentType, DateTime HireDate);

public sealed record EmployeeDetailDto(
    Guid Id, string EmployeeNumber, string FirstName, string LastName,
    string? Email, string? Phone, string? NationalId,
    DateTime? DateOfBirth, DateTime HireDate, DateTime? TerminationDate,
    string? Department, string? Position, Guid? ManagerId,
    string Status, string EmploymentType, int AnnualLeaveEntitlement,
    List<EmployeeListDto>? DirectReports);

public sealed record LeaveRequestListDto(
    Guid Id, Guid EmployeeId, string EmployeeName,
    string LeaveType, string Status,
    DateTime StartDate, DateTime EndDate, int TotalDays,
    string? Reason, DateTime CreatedAt);

public sealed record AttendanceListDto(
    Guid Id, Guid EmployeeId, DateTime Date,
    TimeSpan? CheckIn, TimeSpan? CheckOut,
    string Status, decimal WorkedHours, decimal OvertimeHours);

public sealed record OrgChartDto(
    Guid Id, string FirstName, string LastName,
    string? Department, string? Position, Guid? ManagerId);

public sealed record LeaveBalanceDto(
    Guid EmployeeId, string EmployeeName, int Year,
    int Entitlement, int Used, int Remaining);
