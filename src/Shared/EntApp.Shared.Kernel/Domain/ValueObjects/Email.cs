using System.Text.RegularExpressions;

namespace EntApp.Shared.Kernel.Domain.ValueObjects;

/// <summary>
/// E-posta adresi value object.
/// Format doğrulaması yapar.
/// </summary>
public sealed partial class Email : ValueObject
{
    /// <summary>E-posta adresi.</summary>
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// E-posta oluşturur. Format validasyonu uygular.
    /// </summary>
    /// <exception cref="ArgumentException">Geçersiz format.</exception>
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(value));
        }

        var trimmed = value.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(trimmed))
        {
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));
        }

        return new Email(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Value;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();
}
