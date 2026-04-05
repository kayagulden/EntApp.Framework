using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.HR.Domain.Ids;

public readonly record struct EmployeeId(Guid Value) : IEntityId;
public readonly record struct AttendanceId(Guid Value) : IEntityId;
public readonly record struct LeaveRequestId(Guid Value) : IEntityId;
