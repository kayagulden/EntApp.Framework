using EntApp.Modules.Finance.Domain.Entities;
using EntApp.Modules.Finance.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Finance.Infrastructure.Persistence;

/// <summary>Finance modülü DbContext — schema: fin</summary>
public sealed class FinanceDbContext : BaseDbContext
{
    public const string Schema = "fin";
    protected override string SchemaName => Schema;

    public DbSet<AccountBase> Accounts => Set<AccountBase>();
    public DbSet<InvoiceBase> Invoices => Set<InvoiceBase>();
    public DbSet<InvoiceItemBase> InvoiceItems => Set<InvoiceItemBase>();
    public DbSet<PaymentBase> Payments => Set<PaymentBase>();

    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountBase>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<AccountId>());
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Name);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.TaxNumber).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InvoiceBase>(e =>
        {
            e.ToTable("invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<InvoiceId>());
            e.Property(x => x.AccountId).HasConversion(new StronglyTypedIdValueConverter<AccountId>());
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DueDate);
            e.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.InvoiceType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(a => a.Invoices).HasForeignKey(x => x.AccountId);
            e.Ignore(x => x.RemainingAmount);
        });

        modelBuilder.Entity<InvoiceItemBase>(e =>
        {
            e.ToTable("invoice_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<InvoiceItemId>());
            e.Property(x => x.InvoiceId).HasConversion(new StronglyTypedIdValueConverter<InvoiceId>());
            e.HasIndex(x => x.InvoiceId);
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.TaxRate).HasPrecision(5, 2);
            e.Property(x => x.DiscountRate).HasPrecision(5, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Invoice).WithMany(i => i.Items).HasForeignKey(x => x.InvoiceId);
        });

        modelBuilder.Entity<PaymentBase>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<PaymentId>());
            e.Property(x => x.AccountId).HasConversion(new StronglyTypedIdValueConverter<AccountId>());
            e.Property(x => x.InvoiceId).HasConversion(new StronglyTypedIdValueConverter<InvoiceId>());
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.PaymentDate);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Account).WithMany(a => a.Payments).HasForeignKey(x => x.AccountId);
        });
    }
}
