namespace EntApp.Shared.Kernel.Domain.ValueObjects;

/// <summary>
/// Para birimi ve tutarı temsil eden value object.
/// Aritmetik operatörlerle hesaplama desteği sağlar.
/// </summary>
public sealed class Money : ValueObject, IComparable<Money>
{
    public decimal Amount { get; }

    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Yeni Money oluşturur.
    /// </summary>
    /// <exception cref="ArgumentException">Currency boş veya null ise.</exception>
    public static Money Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty.", nameof(currency));
        }

        if (currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        }

        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <summary>Sıfır tutarlı Money.</summary>
    public static Money Zero(string currency) => Create(0, currency);

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return Create(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return Create(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
        => Create(Amount * factor, Currency);

    public bool IsZero => Amount == 0;

    public bool IsNegative => Amount < 0;

    public bool IsPositive => Amount > 0;

    public int CompareTo(Money? other)
    {
        if (other is null) { return 1; }
        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Add(right);
    }

    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Subtract(right);
    }

    public static Money operator *(Money money, decimal factor)
    {
        ArgumentNullException.ThrowIfNull(money);
        return money.Multiply(factor);
    }

    public static bool operator >(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) <= 0;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot operate on Money with different currencies: {Currency} vs {other.Currency}.");
        }
    }
}
