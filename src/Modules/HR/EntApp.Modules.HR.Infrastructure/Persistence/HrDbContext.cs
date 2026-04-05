using EntApp.Modules.HR.Domain.Entities;
using EntApp.Modules.HR.Domain.Enums;
using EntApp.Modules.HR.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.HR.Infrastructure.Persistence;

/// <summary>HR modülü DbContext — schema: hr</summary>
public sealed class HrDbContext : DbContext
{
    public const string Schema = "hr";

    public DbSet<EmployeeBase> Employees => Set<EmployeeBase>();
    public DbSet<LeaveRequestBase> LeaveRequests => Set<LeaveRequestBase>();
    public DbSet<AttendanceBase> Attendances => Set<AttendanceBase>();

    public HrDbContext(DbContextOptions<HrDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<EmployeeBase>(e =>
        {
            e.ToTable("employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<EmployeeId>());
            e.Property(x => x.ManagerId).HasConversion(new StronglyTypedIdValueConverter<EmployeeId>());
            e.HasIndex(x => x.EmployeeNumber).IsUnique();
            e.HasIndex(x => x.Department);
            e.Property(x => x.EmployeeNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.NationalId).HasMaxLength(11);
            e.Property(x => x.Department).HasMaxLength(100);
            e.Property(x => x.Position).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Manager).WithMany(x => x.DirectReports).HasForeignKey(x => x.ManagerId);
            e.Ignore(x => x.FullName);
        });

        modelBuilder.Entity<LeaveRequestBase>(e =>
        {
            e.ToTable("leave_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<LeaveRequestId>());
            e.Property(x => x.EmployeeId).HasConversion(new StronglyTypedIdValueConverter<EmployeeId>());
            e.HasIndex(x => x.EmployeeId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.LeaveType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.ApproverComment).HasMaxLength(1000);
            e.HasOne(x => x.Employee).WithMany(x => x.LeaveRequests).HasForeignKey(x => x.EmployeeId);
        });

        modelBuilder.Entity<AttendanceBase>(e =>
        {
            e.ToTable("attendances");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<AttendanceId>());
            e.Property(x => x.EmployeeId).HasConversion(new StronglyTypedIdValueConverter<EmployeeId>());
            e.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.WorkedHours).HasPrecision(5, 2);
            e.Property(x => x.OvertimeHours).HasPrecision(5, 2);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Employee).WithMany(x => x.Attendances).HasForeignKey(x => x.EmployeeId);
        });
    }
}
