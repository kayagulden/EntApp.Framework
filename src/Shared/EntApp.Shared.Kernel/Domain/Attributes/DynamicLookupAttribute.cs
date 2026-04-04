namespace EntApp.Shared.Kernel.Domain.Attributes;

/// <summary>
/// Foreign key property'yi lookup (Combobox) olarak işaretler.
/// Frontend'te async arama destekli dropdown olarak render edilir.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class DynamicLookupAttribute : Attribute
{
    /// <summary>
    /// Lookup hedef entity'sinin CLR tipi.
    /// Örnek: typeof(Customer)
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Dropdown'da gösterilecek alan adı.
    /// null ise "Name" convention'ı kullanılır.
    /// </summary>
    public string? DisplayField { get; set; }

    public DynamicLookupAttribute(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        EntityType = entityType;
    }
}
