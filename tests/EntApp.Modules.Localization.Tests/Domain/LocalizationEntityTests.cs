using EntApp.Modules.Localization.Domain.Entities;
using FluentAssertions;

namespace EntApp.Modules.Localization.Tests.Domain;

public class LanguageTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        var lang = Language.Create("tr", "Türkçe", "Türkçe", isDefault: true, displayOrder: 1);

        lang.Code.Should().Be("tr");
        lang.Name.Should().Be("Türkçe");
        lang.NativeName.Should().Be("Türkçe");
        lang.IsDefault.Should().BeTrue();
        lang.IsActive.Should().BeTrue();
        lang.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => Language.Create("", "English", "English");
        act.Should().Throw<ArgumentException>().WithMessage("*Dil kodu*boş*");
    }

    [Fact]
    public void Create_WithShortCode_ShouldThrow()
    {
        var act = () => Language.Create("x", "X", "X");
        act.Should().Throw<ArgumentException>().WithMessage("*2-10*");
    }

    [Fact]
    public void Code_ShouldBeLowercase()
    {
        var lang = Language.Create("EN", "English", "English");
        lang.Code.Should().Be("en");
    }

    [Fact]
    public void SetAsDefault_ShouldSetTrue()
    {
        var lang = Language.Create("en", "English", "English");
        lang.SetAsDefault();
        lang.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ClearDefault_ShouldSetFalse()
    {
        var lang = Language.Create("en", "English", "English", isDefault: true);
        lang.ClearDefault();
        lang.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var lang = Language.Create("en", "English", "English");
        lang.Deactivate();
        lang.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetActive()
    {
        var lang = Language.Create("en", "English", "English");
        lang.Deactivate();
        lang.Activate();
        lang.IsActive.Should().BeTrue();
    }
}

public class TranslationEntryTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        var entry = TranslationEntry.Create("tr", "common", "hello", "Merhaba");

        entry.LanguageCode.Should().Be("tr");
        entry.Namespace.Should().Be("common");
        entry.Key.Should().Be("hello");
        entry.Value.Should().Be("Merhaba");
        entry.IsVerified.Should().BeFalse();
        entry.FullKey.Should().Be("common.hello");
    }

    [Fact]
    public void Create_WithEmptyLanguageCode_ShouldThrow()
    {
        var act = () => TranslationEntry.Create("", "common", "hello", "Hello");
        act.Should().Throw<ArgumentException>().WithMessage("*Dil kodu*");
    }

    [Fact]
    public void Create_WithEmptyKey_ShouldThrow()
    {
        var act = () => TranslationEntry.Create("en", "common", "", "Hello");
        act.Should().Throw<ArgumentException>().WithMessage("*anahtarı*");
    }

    [Fact]
    public void Create_WithNullNamespace_ShouldDefaultToCommon()
    {
        var entry = TranslationEntry.Create("en", null!, "hello", "Hello");
        entry.Namespace.Should().Be("common");
    }

    [Fact]
    public void UpdateValue_ShouldChangeValueAndAudit()
    {
        var entry = TranslationEntry.Create("en", "common", "hello", "Hello");

        entry.UpdateValue("Hi there!", "admin@test.com");

        entry.Value.Should().Be("Hi there!");
        entry.LastModifiedBy.Should().Be("admin@test.com");
        entry.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Verify_ShouldSetTrue()
    {
        var entry = TranslationEntry.Create("en", "common", "hello", "Hello");
        entry.Verify();
        entry.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void Unverify_ShouldSetFalse()
    {
        var entry = TranslationEntry.Create("en", "common", "hello", "Hello");
        entry.Verify();
        entry.Unverify();
        entry.IsVerified.Should().BeFalse();
    }

    [Fact]
    public void LanguageCode_ShouldBeLowercase()
    {
        var entry = TranslationEntry.Create("EN-US", "common", "hello", "Hello");
        entry.LanguageCode.Should().Be("en-us");
    }
}
