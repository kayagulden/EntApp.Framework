using EntApp.Modules.Configuration.Domain.Entities;
using FluentAssertions;

namespace EntApp.Modules.Configuration.Tests.Domain;

public class AppSettingTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        // Arrange & Act
        var setting = AppSetting.Create("SmtpHost", "smtp.example.com", SettingValueType.String,
            description: "SMTP sunucu adresi", group: "Email");

        // Assert
        setting.Key.Should().Be("SmtpHost");
        setting.Value.Should().Be("smtp.example.com");
        setting.ValueType.Should().Be(SettingValueType.String);
        setting.Description.Should().Be("SMTP sunucu adresi");
        setting.Group.Should().Be("Email");
        setting.TenantId.Should().BeNull();
        setting.IsEncrypted.Should().BeFalse();
        setting.IsReadOnly.Should().BeFalse();
        setting.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyKey_ShouldThrow()
    {
        // Act & Assert
        var act = () => AppSetting.Create("", "value", SettingValueType.String);
        act.Should().Throw<ArgumentException>().WithMessage("*anahtarı*boş*");
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var setting = AppSetting.Create("Key", "Value", SettingValueType.String, tenantId: tenantId);

        setting.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithEncrypted_ShouldSetFlag()
    {
        var setting = AppSetting.Create("ApiKey", "secret-123", SettingValueType.String, isEncrypted: true);

        setting.IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void UpdateValue_WhenNotReadOnly_ShouldUpdateValue()
    {
        var setting = AppSetting.Create("MaxUpload", "10", SettingValueType.Int);

        setting.UpdateValue("20");

        setting.Value.Should().Be("20");
    }

    [Fact]
    public void UpdateValue_WhenReadOnly_ShouldThrow()
    {
        var setting = AppSetting.Create("SystemVersion", "1.0", SettingValueType.String, isReadOnly: true);

        var act = () => setting.UpdateValue("2.0");

        act.Should().Throw<InvalidOperationException>().WithMessage("*salt okunur*");
    }

    [Fact]
    public void GetBoolValue_WithTrueString_ShouldReturnTrue()
    {
        var setting = AppSetting.Create("EnableFeature", "true", SettingValueType.Bool);

        setting.GetBoolValue().Should().BeTrue();
    }

    [Fact]
    public void GetBoolValue_WithFalseString_ShouldReturnFalse()
    {
        var setting = AppSetting.Create("EnableFeature", "false", SettingValueType.Bool);

        setting.GetBoolValue().Should().BeFalse();
    }

    [Fact]
    public void GetIntValue_WithValidNumber_ShouldReturnInt()
    {
        var setting = AppSetting.Create("MaxRetries", "5", SettingValueType.Int);

        setting.GetIntValue().Should().Be(5);
    }

    [Fact]
    public void GetIntValue_WithInvalidNumber_ShouldReturnZero()
    {
        var setting = AppSetting.Create("MaxRetries", "abc", SettingValueType.Int);

        setting.GetIntValue().Should().Be(0);
    }

    [Fact]
    public void Create_ShouldTrimKey()
    {
        var setting = AppSetting.Create("  SpacedKey  ", "value", SettingValueType.String);

        setting.Key.Should().Be("SpacedKey");
    }
}
