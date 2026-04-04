namespace EntApp.Modules.HR.Domain.Enums;

/// <summary>Çalışan durumu.</summary>
public enum EmployeeStatus
{
    Active = 0,
    OnLeave = 1,
    Suspended = 2,
    Terminated = 3
}

/// <summary>İstihdam tipi.</summary>
public enum EmploymentType
{
    FullTime = 0,
    PartTime = 1,
    Contract = 2,
    Intern = 3
}

/// <summary>İzin tipi.</summary>
public enum LeaveType
{
    Annual = 0,
    Sick = 1,
    Unpaid = 2,
    Maternity = 3,
    Paternity = 4,
    Marriage = 5,
    Bereavement = 6,
    Administrative = 7
}

/// <summary>İzin talebi durumu.</summary>
public enum LeaveRequestStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

/// <summary>Puantaj durumu.</summary>
public enum AttendanceStatus
{
    Present = 0,
    Absent = 1,
    Late = 2,
    HalfDay = 3,
    Holiday = 4,
    OnLeave = 5
}
