using EntApp.Modules.HR.Domain.Entities;
using EntApp.Modules.HR.Domain.Enums;
using EntApp.Modules.HR.Domain.Ids;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntApp.IntegrationTests;

/// <summary>
/// HR izin talebi → Workflow onay akışı testi.
/// Senaryo: Çalışan izin talep eder, yönetici onaylar, bakiye güncellenir.
/// </summary>
public class HrLeaveWorkflowTests
{
    [Fact]
    public async Task Leave_Request_Approval_Updates_Balance()
    {
        using var db = TestDbFactory.CreateHrDb();

        // Arrange — çalışan oluştur (14 gün hak)
        var employee = EmployeeBase.Create("EMP-001", "Ali", "Yılmaz",
            DateTime.UtcNow.AddYears(-2), EmploymentType.FullTime,
            email: "ali@test.com", department: "Yazılım", annualLeaveEntitlement: 14);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        // Act — izin talebi oluştur ve onayla
        var leave = LeaveRequestBase.Create(employee.Id, LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(9), "Tatil");
        leave.Submit();
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync();

        leave.Status.Should().Be(LeaveRequestStatus.Pending);
        leave.TotalDays.Should().Be(5);

        // Yönetici onaylar
        var managerId = Guid.NewGuid();
        leave.Approve(managerId, "Onaylandı, iyi tatiller!");
        await db.SaveChangesAsync();

        // Assert
        leave.Status.Should().Be(LeaveRequestStatus.Approved);
        leave.ApprovedByUserId.Should().Be(managerId);
        leave.ApproverComment.Should().Be("Onaylandı, iyi tatiller!");

        // Bakiye kontrolü
        var usedDays = await db.LeaveRequests
            .Where(l => l.EmployeeId == employee.Id
                && l.LeaveType == LeaveType.Annual
                && l.Status == LeaveRequestStatus.Approved
                && l.StartDate.Year == DateTime.UtcNow.Year)
            .SumAsync(l => l.TotalDays);

        var remaining = employee.AnnualLeaveEntitlement - usedDays;
        remaining.Should().Be(9); // 14 - 5
    }

    [Fact]
    public async Task Leave_Request_Rejection_Does_Not_Affect_Balance()
    {
        using var db = TestDbFactory.CreateHrDb();

        var employee = EmployeeBase.Create("EMP-002", "Ayşe", "Demir",
            DateTime.UtcNow.AddYears(-1), annualLeaveEntitlement: 14);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var leave = LeaveRequestBase.Create(employee.Id, LeaveType.Annual,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3));
        leave.Submit();
        db.LeaveRequests.Add(leave);
        leave.Reject(Guid.NewGuid(), "Proje teslimi var");
        await db.SaveChangesAsync();

        leave.Status.Should().Be(LeaveRequestStatus.Rejected);

        var usedDays = await db.LeaveRequests
            .Where(l => l.EmployeeId == employee.Id
                && l.Status == LeaveRequestStatus.Approved)
            .SumAsync(l => l.TotalDays);

        usedDays.Should().Be(0);
    }

    [Fact]
    public async Task Org_Chart_Self_Reference_Works()
    {
        using var db = TestDbFactory.CreateHrDb();

        var manager = EmployeeBase.Create("MGR-001", "Mehmet", "Kaya",
            DateTime.UtcNow.AddYears(-5), department: "Yazılım", position: "Müdür");
        db.Employees.Add(manager);
        await db.SaveChangesAsync();

        var dev = EmployeeBase.Create("DEV-001", "Zeynep", "Aydın",
            DateTime.UtcNow.AddYears(-1), department: "Yazılım", position: "Developer",
            managerId: manager.Id);
        db.Employees.Add(dev);
        await db.SaveChangesAsync();

        var mgrWithReports = await db.Employees
            .Include(e => e.DirectReports)
            .FirstAsync(e => e.Id == manager.Id);

        mgrWithReports.DirectReports.Should().HaveCount(1);
        mgrWithReports.DirectReports.First().FullName.Should().Be("Zeynep Aydın");
    }
}
