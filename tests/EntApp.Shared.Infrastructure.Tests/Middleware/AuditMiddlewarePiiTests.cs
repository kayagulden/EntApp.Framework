using EntApp.Shared.Infrastructure.Middleware;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Infrastructure.Tests.Middleware;

public class AuditMiddlewarePiiTests
{
    // ========== MaskEmail ==========

    [Theory]
    [InlineData("user@example.com", "u***@example.com")]
    [InlineData("admin@test.org", "a***@test.org")]
    [InlineData("a@b.com", "a***@b.com")]
    public void MaskEmail_ShouldMaskLocalPart(string input, string expected)
    {
        AuditMiddleware.MaskEmail(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MaskEmail_NullOrEmpty_ShouldReturnStars(string? input)
    {
        AuditMiddleware.MaskEmail(input).Should().Be("***");
    }

    [Fact]
    public void MaskEmail_NoAtSign_ShouldReturnStars()
    {
        AuditMiddleware.MaskEmail("invalid-email").Should().Be("***");
    }

    // ========== MaskPhoneNumber ==========

    [Theory]
    [InlineData("+905321234567", "+90***4567")]
    [InlineData("05321234567", "053***4567")]
    public void MaskPhoneNumber_ShouldMaskMiddle(string input, string expected)
    {
        AuditMiddleware.MaskPhoneNumber(input).Should().Be(expected);
    }

    [Fact]
    public void MaskPhoneNumber_WithFormatting_ShouldCleanAndMask()
    {
        var result = AuditMiddleware.MaskPhoneNumber("+90 532 123 45 67");

        result.Should().Be("+90***4567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12")]
    public void MaskPhoneNumber_TooShort_ShouldReturnStars(string? input)
    {
        AuditMiddleware.MaskPhoneNumber(input).Should().Be("***");
    }

    // ========== MaskIdentityNumber ==========

    [Theory]
    [InlineData("12345678901", "123***901")]
    [InlineData("98765432100", "987***100")]
    public void MaskIdentityNumber_ShouldMaskMiddle(string input, string expected)
    {
        AuditMiddleware.MaskIdentityNumber(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    public void MaskIdentityNumber_TooShort_ShouldReturnStars(string? input)
    {
        AuditMiddleware.MaskIdentityNumber(input).Should().Be("***");
    }
}
