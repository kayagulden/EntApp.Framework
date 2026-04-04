using EntApp.Modules.Sales.Infrastructure.Persistence;
using EntApp.Modules.Finance.Infrastructure.Persistence;
using EntApp.Modules.Inventory.Infrastructure.Persistence;
using EntApp.Modules.HR.Infrastructure.Persistence;
using EntApp.Modules.CRM.Infrastructure.Persistence;
using EntApp.Modules.Procurement.Infrastructure.Persistence;
using EntApp.Modules.TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EntApp.IntegrationTests;

/// <summary>InMemory DbContext factory — test amaçlı.</summary>
public static class TestDbFactory
{
    public static SalesDbContext CreateSalesDb()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new SalesDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static FinanceDbContext CreateFinanceDb()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new FinanceDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static InventoryDbContext CreateInventoryDb()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new InventoryDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static HrDbContext CreateHrDb()
    {
        var options = new DbContextOptionsBuilder<HrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new HrDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static CrmDbContext CreateCrmDb()
    {
        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new CrmDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static ProcurementDbContext CreateProcurementDb()
    {
        var options = new DbContextOptionsBuilder<ProcurementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new ProcurementDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static TaskManagementDbContext CreateTaskManagementDb()
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new TaskManagementDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
