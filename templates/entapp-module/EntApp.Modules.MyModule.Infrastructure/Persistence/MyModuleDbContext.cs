using EntApp.Modules.MyModule.Domain.Entities;
using EntApp.Modules.MyModule.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.MyModule.Infrastructure.Persistence;

/// <summary>MyModule modülü DbContext — schema: moduleschema</summary>
public sealed class MyModuleDbContext : BaseDbContext
{
    public const string Schema = "moduleschema";
    protected override string SchemaName => Schema;

    public DbSet<SampleEntity> SampleEntities => Set<SampleEntity>();

    public MyModuleDbContext(DbContextOptions<MyModuleDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SampleEntity>(e =>
        {
            e.ToTable("sample_entities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<SampleEntityId>());
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}
