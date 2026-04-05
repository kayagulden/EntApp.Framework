using System.Diagnostics.CodeAnalysis;

namespace EntApp.Shared.Kernel.Domain;

/// <summary>
/// Strongly Typed ID base record struct.
/// Compile-time'da farklı entity ID'lerinin karışmasını engeller.
/// </summary>
/// <example>
/// <code>
/// public readonly record struct CustomerId(Guid Value) : IEntityId;
/// public readonly record struct OrderId(Guid Value) : IEntityId;
/// </code>
/// </example>
public interface IEntityId
{
    Guid Value { get; }
}

/// <summary>
/// Yeni bir strongly typed ID oluşturmak için static factory yardımcısı.
/// </summary>
public static class EntityId
{
    public static T New<T>() where T : struct, IEntityId
        => Create<T>(Guid.CreateVersion7());

    public static T From<T>(Guid value) where T : struct, IEntityId
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Entity ID cannot be empty.", nameof(value));
        }

        return Create<T>(value);
    }

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Factory method")]
    private static T Create<T>(Guid value) where T : struct, IEntityId
        => (T)Activator.CreateInstance(typeof(T), value)!;
}
