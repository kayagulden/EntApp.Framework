namespace EntApp.Shared.Kernel.Domain.ValueObjects;

/// <summary>
/// Adres value object.
/// </summary>
public sealed class Address : ValueObject
{
    public string Street { get; }

    public string City { get; }

    public string State { get; }

    public string Country { get; }

    public string ZipCode { get; }

    private Address(string street, string city, string state, string country, string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
    }

    public static Address Create(string street, string city, string state, string country, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new ArgumentException("Street cannot be empty.", nameof(street));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City cannot be empty.", nameof(city));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country cannot be empty.", nameof(country));
        }

        return new Address(street.Trim(), city.Trim(), state?.Trim() ?? string.Empty, country.Trim(), zipCode?.Trim() ?? string.Empty);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }

    public override string ToString()
        => $"{Street}, {City}, {State} {ZipCode}, {Country}";
}
