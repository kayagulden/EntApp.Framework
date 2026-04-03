using EntApp.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public const string Schema = "notification";

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationTemplate> Templates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> Logs => Set<NotificationLog>();
    public DbSet<UserNotificationPreference> Preferences => Set<UserNotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<NotificationTemplate>(e =>
        {
            e.ToTable("Templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.Language).HasMaxLength(10);
            e.HasIndex(x => new { x.Code, x.Channel, x.TenantId }).IsUnique();
        });

        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.ToTable("Logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Recipient).HasMaxLength(300).IsRequired();
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.TemplateCode).HasMaxLength(100);
            e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.SentAt);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<UserNotificationPreference>(e =>
        {
            e.ToTable("Preferences");
            e.HasKey(x => x.Id);
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => new { x.UserId, x.Channel, x.TenantId }).IsUnique();
        });
    }
}
