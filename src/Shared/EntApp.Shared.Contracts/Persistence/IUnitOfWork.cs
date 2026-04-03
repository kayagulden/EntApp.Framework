namespace EntApp.Shared.Contracts.Persistence;

/// <summary>
/// Unit of Work kontratı.
/// Her modülün DbContext'i bu interface'i implement eder.
/// TransactionBehavior bu interface üzerinden transaction yönetir.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Değişiklikleri veritabanına kaydeder.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Yeni bir transaction başlatır.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Aktif transaction'ı commit eder.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Aktif transaction'ı geri alır.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
