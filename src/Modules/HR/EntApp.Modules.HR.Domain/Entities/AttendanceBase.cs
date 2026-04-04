using EntApp.Modules.HR.Domain.Enums;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.HR.Domain.Entities;

/// <summary>Puantaj kaydı — günlük giriş/çıkış.</summary>
public sealed class AttendanceBase : AuditableEntity<Guid>, ITenantEntity
{
    public Guid EmployeeId { get; private set; }

    public DateTime Date { get; private set; }
    public TimeSpan? CheckIn { get; private set; }
    public TimeSpan? CheckOut { get; private set; }

    public AttendanceStatus Status { get; private set; } = AttendanceStatus.Present;

    /// <summary>Çalışılan saat</summary>
    public decimal WorkedHours { get; private set; }

    /// <summary>Fazla mesai saati</summary>
    public decimal OvertimeHours { get; private set; }

    public string? Notes { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public EmployeeBase Employee { get; private set; } = null!;

    private AttendanceBase() { }

    public static AttendanceBase Create(
        Guid employeeId, DateTime date,
        TimeSpan? checkIn = null, TimeSpan? checkOut = null,
        AttendanceStatus status = AttendanceStatus.Present,
        string? notes = null)
    {
        decimal workedHours = 0;
        if (checkIn.HasValue && checkOut.HasValue)
            workedHours = (decimal)(checkOut.Value - checkIn.Value).TotalHours;

        return new AttendanceBase
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Date = date.Date,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Status = status,
            WorkedHours = Math.Round(workedHours, 2),
            OvertimeHours = Math.Max(0, Math.Round(workedHours - 8, 2)),
            Notes = notes
        };
    }
}
