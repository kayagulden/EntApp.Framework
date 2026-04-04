namespace EntApp.Shared.Kernel.Domain.Attributes;

/// <summary>
/// Entity'yi Dynamic CRUD sistemi için işaretler.
/// Bu attribute bulunan entity'ler için otomatik metadata API
/// ve generic CRUD endpoint'leri oluşturulur.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DynamicEntityAttribute : Attribute
{
    /// <summary>
    /// Entity'nin API ve metadata'da kullanılacak adı.
    /// URL: /api/v1/dynamic/{Name}
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Sidebar'da altında gösterileceği menu grubu (ör: "Tanımlar", "Satış").
    /// null ise "Genel" grubuna dahil edilir.
    /// </summary>
    public string? MenuGroup { get; set; }

    /// <summary>
    /// true ise bu entity bir master-detail ilişkisinin detail (alt) tarafıdır.
    /// Detail entity'ler için bağımsız menu öğesi oluşturulmaz.
    /// </summary>
    public bool IsDetail { get; set; }

    public DynamicEntityAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
