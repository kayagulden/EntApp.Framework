using System.Reflection;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// [DynamicEntity] attribute'ü olan entity'lerin kayıt defteri.
/// Startup'ta assembly taraması ile doldurulur.
/// </summary>
public interface IDynamicEntityRegistry
{
    /// <summary>Tüm kayıtlı dynamic entity bilgilerini döner.</summary>
    IReadOnlyList<DynamicEntityInfo> GetAll();

    /// <summary>Entity adına göre bilgi döner.</summary>
    DynamicEntityInfo? GetByName(string entityName);

    /// <summary>CLR tipine göre bilgi döner.</summary>
    DynamicEntityInfo? GetByType(Type entityType);

    /// <summary>Assembly taraması ile entity'leri kaydeder.</summary>
    void ScanAssemblies(params Assembly[] assemblies);
}

/// <summary>
/// Kayıtlı dynamic entity bilgisi.
/// </summary>
public sealed record DynamicEntityInfo
{
    /// <summary>Entity adı (URL'de kullanılır).</summary>
    public required string Name { get; init; }

    /// <summary>CLR tipi.</summary>
    public required Type ClrType { get; init; }

    /// <summary>Menu grubu.</summary>
    public string? MenuGroup { get; init; }

    /// <summary>Detail entity mi?</summary>
    public bool IsDetail { get; init; }

    /// <summary>DynamicEntity attribute referansı.</summary>
    public required DynamicEntityAttribute Attribute { get; init; }
}
