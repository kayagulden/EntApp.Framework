using EntApp.Modules.HR.Domain.Enums;
using EntApp.Modules.HR.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.HR.Domain.Entities;

/// <summary>Çalışan — HR modülünün temel entity'si.</summary>
[DynamicEntity("Employee", MenuGroup = "İnsan Kaynakları")]
public sealed class EmployeeBase : AuditableEntity<EmployeeId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string EmployeeNumber { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 100, Searchable = true)]
    public string FirstName { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 100, Searchable = true)]
    public string LastName { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, MaxLength = 200, Searchable = true)]
    public string? Email { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? Phone { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 11)]
    public string? NationalId { get; private set; }

    public DateTime? DateOfBirth { get; private set; }
    public DateTime HireDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100, Searchable = true)]
    public string? Department { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? Position { get; private set; }

    /// <summary>Üst yönetici (organizasyon şeması)</summary>
    public EmployeeId? ManagerId { get; private set; }

    public EmployeeStatus Status { get; private set; } = EmployeeStatus.Active;
    public EmploymentType EmploymentType { get; private set; } = EmploymentType.FullTime;

    /// <summary>Yıllık izin hakkı (gün)</summary>
    public int AnnualLeaveEntitlement { get; private set; } = 14;

    public Guid TenantId { get; set; }

    // Navigation
    public EmployeeBase? Manager { get; private set; }
    public ICollection<EmployeeBase> DirectReports { get; private set; } = [];
    public ICollection<LeaveRequestBase> LeaveRequests { get; private set; } = [];
    public ICollection<AttendanceBase> Attendances { get; private set; } = [];

    private EmployeeBase() { }

    public static EmployeeBase Create(
        string employeeNumber, string firstName, string lastName,
        DateTime hireDate, EmploymentType employmentType = EmploymentType.FullTime,
        string? email = null, string? phone = null, string? nationalId = null,
        DateTime? dateOfBirth = null, string? department = null,
        string? position = null, EmployeeId? managerId = null,
        int annualLeaveEntitlement = 14)
    {
        return new EmployeeBase
        {
            Id = EntityId.New<EmployeeId>(),
            EmployeeNumber = employeeNumber, FirstName = firstName, LastName = lastName,
            HireDate = hireDate, EmploymentType = employmentType,
            Email = email, Phone = phone, NationalId = nationalId,
            DateOfBirth = dateOfBirth, Department = department,
            Position = position, ManagerId = managerId,
            AnnualLeaveEntitlement = annualLeaveEntitlement
        };
    }

    public void Terminate(DateTime terminationDate)
    {
        Status = EmployeeStatus.Terminated;
        TerminationDate = terminationDate;
    }

    public string FullName => $"{FirstName} {LastName}";
}
