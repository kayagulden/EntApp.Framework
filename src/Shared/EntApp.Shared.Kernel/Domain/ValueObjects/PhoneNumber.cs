using System.Text.RegularExpressions;

namespace EntApp.Shared.Kernel.Domain.ValueObjects;

/// <summary>
/// Telefon numarası value object.
/// Ülke kodu ve numara olmak üzere iki bileşenden oluşur.
/// </summary>
public sealed partial class PhoneNumber : ValueObject
{
    /// <summary>Ülke kodu (ör: +90, +1).</summary>
    public string CountryCode { get; }

    /// <summary>Telefon numarası (ör: 5321234567).</summary>
    public string Number { get; }

    private PhoneNumber(string countryCode, string number)
    {
        CountryCode = countryCode;
        Number = number;
    }

    /// <summary>
    /// Telefon numarası oluşturur.
    /// </summary>
    /// <param name="countryCode">Ülke kodu (+ ile başlar, ör: +90)</param>
    /// <param name="number">Numara (sadece rakam, 4-15 karakter)</param>
    public static PhoneNumber Create(string countryCode, string number)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException("Country code cannot be empty.", nameof(countryCode));
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Phone number cannot be empty.", nameof(number));
        }

        var trimmedCode = countryCode.Trim();
        if (!CountryCodeRegex().IsMatch(trimmedCode))
        {
            throw new ArgumentException($"Invalid country code format: {countryCode}", nameof(countryCode));
        }

        // Numaradan boşluk ve tireyi temizle
        var cleanNumber = number.Replace(" ", "", StringComparison.Ordinal)
                                .Replace("-", "", StringComparison.Ordinal)
                                .Trim();

        if (!NumberRegex().IsMatch(cleanNumber))
        {
            throw new ArgumentException($"Invalid phone number format: {number}", nameof(number));
        }

        return new PhoneNumber(trimmedCode, cleanNumber);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return Number;
    }

    public override string ToString() => $"{CountryCode} {Number}";

    [GeneratedRegex(@"^\+\d{1,4}$")]
    private static partial Regex CountryCodeRegex();

    [GeneratedRegex(@"^\d{4,15}$")]
    private static partial Regex NumberRegex();
}
