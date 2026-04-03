using EntApp.Shared.Contracts.Identity;
using EntApp.Shared.Contracts.Persistence;
using EntApp.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntApp.Shared.Infrastructure.Persistence;

/// <summary>
/// Tüm modül DbContext'lerinin base sınıfı.
/// Soft delete global filter, tenant global filter, AsSplitQuery varsayılan.
/// IUnitOfWork implementasyonu ile transaction yönetimi sağlar.
/// </summary>
public abstract class BaseDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentTenant? _currentTenant;
    private IDbContextTransaction? _currentTransaction;

    protected BaseDbContext(DbContextOptions options) : base(options) { }

    protected BaseDbContext(DbContextOptions options, ICurrentTenant? currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    /// <summary>Modülün DB schema adı (ör: "iam", "crm").</summary>
    protected abstract string SchemaName { get; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // AsSplitQuery varsayılan — TPT JOIN performansı için
        optionsBuilder.UseNpgsql(o
            => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        // Schema ayarı
        modelBuilder.HasDefaultSchema(SchemaName);

        // Tüm entity'ler için soft delete global filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity<>).IsAssignableFrom(entityType.ClrType.BaseType?.GetGenericTypeDefinition() ?? typeof(object)))
            {
                // Soft delete ve tenant filter'lar concrete DbContext'lerde uygulanır
                // çünkü generic base type constraint'leri model builder'da doğrudan kullanılamaz.
                // Bu alan modül DbContext'lerinde override edilecek.
            }
        }

        // Modüle özel konfigürasyonları uygula
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    /// <summary>
    /// Soft delete global filter'ı belirli bir entity tipi için uygular.
    /// Modül DbContext'lerinde çağrılmalıdır.
    /// </summary>
    protected void ApplySoftDeleteFilter<TEntity, TId>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity<TId>
        where TId : struct
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Tenant filter'ı belirli bir entity tipi için uygular.
    /// Multi-tenant entity'lerde çağrılmalıdır.
    /// </summary>
    protected void ApplyTenantFilter<TEntity, TId>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity<TId>, ITenantEntity
        where TId : struct
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        if (_currentTenant?.IsAvailable == true)
        {
            var tenantId = _currentTenant.TenantId;
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == tenantId);
        }
        else
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }
    }

    // ========== IUnitOfWork ==========

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction ??= await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public override void Dispose()
    {
        _currentTransaction?.Dispose();
        base.Dispose();
    }
}
