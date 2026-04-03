namespace EntApp.Shared.Kernel.Domain.ValueObjects;

/// <summary>
/// Tarih aralığı value object.
/// Start ve End tarihleri arasında validasyon, Contains ve Overlaps desteği.
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateTime Start { get; }

    public DateTime End { get; }

    private DateRange(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Yeni DateRange oluşturur.
    /// </summary>
    /// <exception cref="ArgumentException">Start, End'den büyük veya eşit ise.</exception>
    public static DateRange Create(DateTime start, DateTime end)
    {
        if (start >= end)
        {
            throw new ArgumentException("Start date must be before end date.", nameof(start));
        }

        return new DateRange(start, end);
    }

    /// <summary>Verilen tarih bu aralık içinde mi?</summary>
    public bool Contains(DateTime date)
        => date >= Start && date <= End;

    /// <summary>Bu aralık başka bir aralıkla çakışıyor mu?</summary>
    public bool Overlaps(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && other.Start < End;
    }

    /// <summary>Aralığın toplam gün sayısı.</summary>
    public int TotalDays => (int)(End - Start).TotalDays;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} → {End:yyyy-MM-dd}";
}
