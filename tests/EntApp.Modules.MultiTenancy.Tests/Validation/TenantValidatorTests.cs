using EntApp.Modules.MultiTenancy.Application.Commands;
using EntApp.Modules.MultiTenancy.Application.Validators;
using FluentAssertions;

namespace EntApp.Modules.MultiTenancy.Tests.Validation;

public class TenantValidatorTests
{
    [Fact]
    public void CreateTenant_Valid_ShouldPass()
    {
        var validator = new CreateTenantValidator();
        var cmd = new CreateTenantCommand("Acme", "acme-corp", "admin@acme.com", Plan: "Enterprise");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTenant_EmptyName_ShouldFail()
    {
        var validator = new CreateTenantValidator();
        var cmd = new CreateTenantCommand("", "acme-corp");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTenant_ShortIdentifier_ShouldFail()
    {
        var validator = new CreateTenantValidator();
        var cmd = new CreateTenantCommand("Acme", "ab");

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Identifier");
    }

    [Fact]
    public void CreateTenant_InvalidIdentifierChars_ShouldFail()
    {
        var validator = new CreateTenantValidator();
        var cmd = new CreateTenantCommand("Acme", "UPPER_CASE!!");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTenant_InvalidEmail_ShouldFail()
    {
        var validator = new CreateTenantValidator();
        var cmd = new CreateTenantCommand("Acme", "acme-corp", AdminEmail: "not-an-email");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpsertSetting_Valid_ShouldPass()
    {
        var validator = new UpsertSettingValidator();
        var cmd = new UpsertTenantSettingCommand(Guid.NewGuid(), "MaxUsers", "100");

        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpsertSetting_EmptyKey_ShouldFail()
    {
        var validator = new UpsertSettingValidator();
        var cmd = new UpsertTenantSettingCommand(Guid.NewGuid(), "", "100");

        validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
