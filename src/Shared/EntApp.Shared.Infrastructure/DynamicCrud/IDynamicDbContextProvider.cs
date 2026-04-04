using Microsoft.EntityFrameworkCore;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// Entity adına göre doğru DbContext instance'ını resolve eder.
/// Her modül kendi DbContext'ini bu provider'a kaydeder.
/// </summary>
public interface IDynamicDbContextProvider
{
    /// <summary>Belirtilen entity tipi için DbContext döner.</summary>
    DbContext GetDbContext(Type entityType);

    /// <summary>Kayıtlı entity-DbContext eşleştirmesini ekler.</summary>
    void Register(Type entityType, Type dbContextType);

    /// <summary>Entity tipi kayıtlı mı?</summary>
    bool IsRegistered(Type entityType);
}
