using EntApp.Modules.Localization.Application.Commands;
using EntApp.Modules.Localization.Application.Validators;
using FluentAssertions;

namespace EntApp.Modules.Localization.Tests.Validation;

public class LocalizationValidatorTests
{
    [Fact]
    public void CreateLanguage_Valid_ShouldPass()
    {
        var validator = new CreateLanguageValidator();
        var cmd = new CreateLanguageCommand("tr", "Türkçe", "Türkçe");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateLanguage_ValidWithRegion_ShouldPass()
    {
        var validator = new CreateLanguageValidator();
        var cmd = new CreateLanguageCommand("en-US", "English (US)", "English");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateLanguage_InvalidCode_ShouldFail()
    {
        var validator = new CreateLanguageValidator();
        var cmd = new CreateLanguageCommand("123", "Invalid", "Invalid");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateLanguage_EmptyName_ShouldFail()
    {
        var validator = new CreateLanguageValidator();
        var cmd = new CreateLanguageCommand("en", "", "English");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpsertTranslation_Valid_ShouldPass()
    {
        var validator = new UpsertTranslationValidator();
        var cmd = new UpsertTranslationCommand("tr", "common", "hello", "Merhaba");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpsertTranslation_EmptyKey_ShouldFail()
    {
        var validator = new UpsertTranslationValidator();
        var cmd = new UpsertTranslationCommand("tr", "common", "", "Merhaba");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpsertTranslation_EmptyNamespace_ShouldFail()
    {
        var validator = new UpsertTranslationValidator();
        var cmd = new UpsertTranslationCommand("tr", "", "hello", "Merhaba");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
