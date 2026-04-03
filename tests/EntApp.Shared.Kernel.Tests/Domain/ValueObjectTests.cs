using EntApp.Shared.Kernel.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Kernel.Tests.Domain;

public class ValueObjectTests
{
    // ========== Money Tests ==========

    [Fact]
    public void Money_SameValues_ShouldBeEqual()
    {
        var money1 = Money.Create(100m, "TRY");
        var money2 = Money.Create(100m, "TRY");

        money1.Should().Be(money2);
    }

    [Fact]
    public void Money_DifferentAmount_ShouldNotBeEqual()
    {
        var money1 = Money.Create(100m, "TRY");
        var money2 = Money.Create(200m, "TRY");

        money1.Should().NotBe(money2);
    }

    [Fact]
    public void Money_Add_ShouldWork()
    {
        var a = Money.Create(100m, "TRY");
        var b = Money.Create(50m, "TRY");

        var result = a + b;

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Money_Subtract_ShouldWork()
    {
        var a = Money.Create(100m, "TRY");
        var b = Money.Create(30m, "TRY");

        var result = a - b;

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Money_DifferentCurrency_ShouldThrow()
    {
        var tryMoney = Money.Create(100m, "TRY");
        var usdMoney = Money.Create(50m, "USD");

        var act = () => tryMoney + usdMoney;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Money_InvalidCurrency_ShouldThrow()
    {
        var act = () => Money.Create(100m, "TRYY");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*ISO 4217*");
    }

    [Fact]
    public void Money_Comparison_ShouldWork()
    {
        var small = Money.Create(50m, "USD");
        var large = Money.Create(100m, "USD");

        (large > small).Should().BeTrue();
        (small < large).Should().BeTrue();
        var smallCopy = Money.Create(50m, "USD");
        var largeCopy = Money.Create(100m, "USD");
        (smallCopy >= small).Should().BeTrue();
        (largeCopy <= large).Should().BeTrue();
    }

    [Fact]
    public void Money_Zero_ShouldBeZero()
    {
        var zero = Money.Zero("USD");

        zero.IsZero.Should().BeTrue();
        zero.Amount.Should().Be(0);
    }

    // ========== Email Tests ==========

    [Fact]
    public void Email_ValidFormat_ShouldCreateSuccessfully()
    {
        var email = Email.Create("test@example.com");

        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Email_ShouldNormalizeToLowercase()
    {
        var email = Email.Create("Test@EXAMPLE.COM");

        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Email_InvalidFormat_ShouldThrow()
    {
        var act = () => Email.Create("not-an-email");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email*");
    }

    [Fact]
    public void Email_Empty_ShouldThrow()
    {
        var act = () => Email.Create("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Email_ImplicitStringConversion_ShouldWork()
    {
        var email = Email.Create("user@domain.com");

        string value = email;

        value.Should().Be("user@domain.com");
    }

    // ========== DateRange Tests ==========

    [Fact]
    public void DateRange_ValidRange_ShouldCreate()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var range = DateRange.Create(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void DateRange_InvalidRange_ShouldThrow()
    {
        var act = () => DateRange.Create(
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*before end*");
    }

    [Fact]
    public void DateRange_Contains_ShouldWork()
    {
        var range = DateRange.Create(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        range.Contains(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
        range.Contains(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    [Fact]
    public void DateRange_Overlaps_ShouldDetect()
    {
        var range1 = DateRange.Create(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));
        var range2 = DateRange.Create(
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc));
        var range3 = DateRange.Create(
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        range1.Overlaps(range2).Should().BeTrue();
        range1.Overlaps(range3).Should().BeFalse();
    }

    [Fact]
    public void DateRange_TotalDays_ShouldCalculate()
    {
        var range = DateRange.Create(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc));

        range.TotalDays.Should().Be(10);
    }

    // ========== Address Tests ==========

    [Fact]
    public void Address_ValidData_ShouldCreate()
    {
        var address = Address.Create("Atatürk Cd. 123", "Ankara", "Çankaya", "Türkiye", "06100");

        address.Street.Should().Be("Atatürk Cd. 123");
        address.City.Should().Be("Ankara");
        address.Country.Should().Be("Türkiye");
    }

    [Fact]
    public void Address_EmptyStreet_ShouldThrow()
    {
        var act = () => Address.Create("", "Ankara", "Çankaya", "Türkiye", "06100");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Address_SameValues_ShouldBeEqual()
    {
        var addr1 = Address.Create("St 1", "City", "State", "Country", "12345");
        var addr2 = Address.Create("St 1", "City", "State", "Country", "12345");

        addr1.Should().Be(addr2);
    }

    // ========== PhoneNumber Tests ==========

    [Fact]
    public void PhoneNumber_ValidData_ShouldCreate()
    {
        var phone = PhoneNumber.Create("+90", "5321234567");

        phone.CountryCode.Should().Be("+90");
        phone.Number.Should().Be("5321234567");
    }

    [Fact]
    public void PhoneNumber_WithFormatting_ShouldClean()
    {
        var phone = PhoneNumber.Create("+90", "532 123 45 67");

        phone.Number.Should().Be("5321234567");
    }

    [Fact]
    public void PhoneNumber_InvalidCountryCode_ShouldThrow()
    {
        var act = () => PhoneNumber.Create("90", "5321234567");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*country code*");
    }

    [Fact]
    public void PhoneNumber_TooShortNumber_ShouldThrow()
    {
        var act = () => PhoneNumber.Create("+90", "123");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*phone number*");
    }
}
