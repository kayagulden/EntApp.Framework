using EntApp.Modules.Configuration.Application.Commands;
using EntApp.Modules.Configuration.Application.Validators;
using FluentAssertions;

namespace EntApp.Modules.Configuration.Tests.Validation;

public class ValidatorTests
{
    [Fact]
    public void UpsertAppSetting_ValidCommand_ShouldPass()
    {
        var validator = new UpsertAppSettingValidator();
        var cmd = new UpsertAppSettingCommand("SmtpHost", "smtp.example.com", "String");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpsertAppSetting_EmptyKey_ShouldFail()
    {
        var validator = new UpsertAppSettingValidator();
        var cmd = new UpsertAppSettingCommand("", "value", "String");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Key");
    }

    [Fact]
    public void UpsertAppSetting_InvalidValueType_ShouldFail()
    {
        var validator = new UpsertAppSettingValidator();
        var cmd = new UpsertAppSettingCommand("Key", "value", "InvalidType");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ValueType");
    }

    [Fact]
    public void UpsertAppSetting_TooLongKey_ShouldFail()
    {
        var validator = new UpsertAppSettingValidator();
        var cmd = new UpsertAppSettingCommand(new string('A', 201), "value", "String");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateFeatureFlag_ValidCommand_ShouldPass()
    {
        var validator = new CreateFeatureFlagValidator();
        var cmd = new CreateFeatureFlagCommand("MaintenanceMode", "Bakım Modu");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateFeatureFlag_EmptyName_ShouldFail()
    {
        var validator = new CreateFeatureFlagValidator();
        var cmd = new CreateFeatureFlagCommand("", "Display");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateFeatureFlag_EmptyDisplayName_ShouldFail()
    {
        var validator = new CreateFeatureFlagValidator();
        var cmd = new CreateFeatureFlagCommand("Name", "");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public void CreateFeatureFlag_TooLongName_ShouldFail()
    {
        var validator = new CreateFeatureFlagValidator();
        var cmd = new CreateFeatureFlagCommand(new string('A', 201), "Display");

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }
}
