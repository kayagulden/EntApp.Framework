using EntApp.Modules.Configuration.Domain.Entities;
using FluentAssertions;

namespace EntApp.Modules.Configuration.Tests.Domain;

public class FeatureFlagTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        var flag = FeatureFlag.Create("MaintenanceMode", "Bakım Modu",
            description: "Sistem bakım modunda olduğunda aktifleşir");

        flag.Name.Should().Be("MaintenanceMode");
        flag.DisplayName.Should().Be("Bakım Modu");
        flag.Description.Should().Be("Sistem bakım modunda olduğunda aktifleşir");
        flag.IsEnabled.Should().BeFalse();
        flag.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => FeatureFlag.Create("", "Display");
        act.Should().Throw<ArgumentException>().WithMessage("*Flag adı*boş*");
    }

    [Fact]
    public void Create_WithEnabledTrue_ShouldBeEnabled()
    {
        var flag = FeatureFlag.Create("NewDashboard", "Yeni Dashboard", isEnabled: true);

        flag.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Toggle_ShouldFlipState()
    {
        var flag = FeatureFlag.Create("Feature", "Display");
        flag.IsEnabled.Should().BeFalse();

        flag.Toggle();
        flag.IsEnabled.Should().BeTrue();

        flag.Toggle();
        flag.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledTrue()
    {
        var flag = FeatureFlag.Create("Feature", "Display");

        flag.Enable();

        flag.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledFalse()
    {
        var flag = FeatureFlag.Create("Feature", "Display", isEnabled: true);

        flag.Disable();

        flag.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEffectivelyEnabled_WhenDisabled_ShouldReturnFalse()
    {
        var flag = FeatureFlag.Create("Feature", "Display");

        flag.IsEffectivelyEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsEffectivelyEnabled_WhenEnabled_NoSchedule_ShouldReturnTrue()
    {
        var flag = FeatureFlag.Create("Feature", "Display", isEnabled: true);

        flag.IsEffectivelyEnabled().Should().BeTrue();
    }

    [Fact]
    public void IsEffectivelyEnabled_WhenEnabled_FutureStart_ShouldReturnFalse()
    {
        var flag = FeatureFlag.Create("Feature", "Display", isEnabled: true);
        flag.SetSchedule(from: DateTime.UtcNow.AddHours(1), until: null);

        flag.IsEffectivelyEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsEffectivelyEnabled_WhenEnabled_PastEnd_ShouldReturnFalse()
    {
        var flag = FeatureFlag.Create("Feature", "Display", isEnabled: true);
        flag.SetSchedule(from: null, until: DateTime.UtcNow.AddHours(-1));

        flag.IsEffectivelyEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsEffectivelyEnabled_WhenEnabled_WithinSchedule_ShouldReturnTrue()
    {
        var flag = FeatureFlag.Create("Feature", "Display", isEnabled: true);
        flag.SetSchedule(
            from: DateTime.UtcNow.AddHours(-1),
            until: DateTime.UtcNow.AddHours(1));

        flag.IsEffectivelyEnabled().Should().BeTrue();
    }

    [Fact]
    public void SetSchedule_ShouldUpdateDates()
    {
        var flag = FeatureFlag.Create("Feature", "Display");
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var until = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        flag.SetSchedule(from, until);

        flag.EnabledFrom.Should().Be(from);
        flag.EnabledUntil.Should().Be(until);
    }

    [Fact]
    public void SetAllowedRoles_ShouldStore()
    {
        var flag = FeatureFlag.Create("Feature", "Display");

        flag.SetAllowedRoles("[\"Admin\",\"Manager\"]");

        flag.AllowedRoles.Should().Be("[\"Admin\",\"Manager\"]");
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var flag = FeatureFlag.Create("Feature", "Display", tenantId: tenantId);

        flag.TenantId.Should().Be(tenantId);
    }
}
