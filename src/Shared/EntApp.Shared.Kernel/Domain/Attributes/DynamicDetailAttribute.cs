namespace EntApp.Shared.Kernel.Domain.Attributes;

/// <summary>
/// Master-detail ilişkisindeki collection navigation property'yi işaretler.
/// Frontend'te master formun altında inline tablo olarak render edilir.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class DynamicDetailAttribute : Attribute
{
    /// <summary>
    /// Detail (alt) entity'nin CLR tipi.
    /// Örnek: typeof(OrderItem)
    /// </summary>
    public Type DetailEntityType { get; }

    public DynamicDetailAttribute(Type detailEntityType)
    {
        ArgumentNullException.ThrowIfNull(detailEntityType);
        DetailEntityType = detailEntityType;
    }
}
