using EntApp.Modules.MultiTenancy.Domain.Entities;
using FluentAssertions;

namespace EntApp.Modules.MultiTenancy.Tests.Domain;

public class TenantTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        var tenant = Tenant.Create("Acme Corp", "acme-corp", "admin@acme.com", "ACME", "Acme Corporation", "Enterprise");

        tenant.Name.Should().Be("Acme Corp");
        tenant.Identifier.Should().Be("acme-corp");
        tenant.AdminEmail.Should().Be("admin@acme.com");
        tenant.Plan.Should().Be("Enterprise");
        tenant.Status.Should().Be(TenantStatus.PendingSetup);
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => Tenant.Create("", "identifier");
        act.Should().Throw<ArgumentException>().WithMessage("*Tenant adı*");
    }

    [Fact]
    public void Create_WithEmptyIdentifier_ShouldThrow()
    {
        var act = () => Tenant.Create("Name", "");
        act.Should().Throw<ArgumentException>().WithMessage("*tanımlayıcı*");
    }

    [Fact]
    public void Create_WithShortIdentifier_ShouldThrow()
    {
        var act = () => Tenant.Create("Name", "ab");
        act.Should().Throw<ArgumentException>().WithMessage("*3-50*");
    }

    [Fact]
    public void Identifier_ShouldBeLowercase()
    {
        var tenant = Tenant.Create("Name", "UPPER-CASE");
        tenant.Identifier.Should().Be("upper-case");
    }

    [Fact]
    public void Activate_ShouldSetActiveStatus()
    {
        var tenant = Tenant.Create("Name", "test-tenant");

        tenant.Activate();

        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.IsActive.Should().BeTrue();
        tenant.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Suspend_ShouldSetSuspendedStatus()
    {
        var tenant = Tenant.Create("Name", "test-tenant");
        tenant.Activate();

        tenant.Suspend("Non-payment");

        tenant.Status.Should().Be(TenantStatus.Suspended);
        tenant.SuspendedAt.Should().NotBeNull();
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldSetDeactivatedStatus()
    {
        var tenant = Tenant.Create("Name", "test-tenant");

        tenant.Deactivate();

        tenant.Status.Should().Be(TenantStatus.Deactivated);
    }

    [Fact]
    public void ChangePlan_ShouldUpdatePlan()
    {
        var tenant = Tenant.Create("Name", "test-tenant");

        tenant.ChangePlan("Premium");

        tenant.Plan.Should().Be("Premium");
    }

    [Fact]
    public void ChangePlan_WithEmpty_ShouldThrow()
    {
        var tenant = Tenant.Create("Name", "test-tenant");
        var act = () => tenant.ChangePlan("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetSubdomain_ShouldNormalize()
    {
        var tenant = Tenant.Create("Name", "test-tenant");

        tenant.SetSubdomain("ACME");

        tenant.Subdomain.Should().Be("acme");
    }

    [Fact]
    public void AddSetting_New_ShouldAdd()
    {
        var tenant = Tenant.Create("Name", "test-tenant");

        tenant.AddSetting("Theme", "dark");

        tenant.GetSetting("Theme").Should().Be("dark");
    }

    [Fact]
    public void AddSetting_Existing_ShouldUpdate()
    {
        var tenant = Tenant.Create("Name", "test-tenant");
        tenant.AddSetting("Theme", "dark");

        tenant.AddSetting("theme", "light"); // case insensitive

        tenant.GetSetting("Theme").Should().Be("light");
        tenant.Settings.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateInfo_ShouldSetFields()
    {
        var tenant = Tenant.Create("Name", "test-tenant");

        tenant.UpdateInfo("Display Name", "Description", "https://logo.png");

        tenant.DisplayName.Should().Be("Display Name");
        tenant.Description.Should().Be("Description");
        tenant.LogoUrl.Should().Be("https://logo.png");
    }
}

public class TenantSettingTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetProperties()
    {
        var setting = TenantSetting.Create(Guid.NewGuid(), "MaxUsers", "100");

        setting.Key.Should().Be("MaxUsers");
        setting.Value.Should().Be("100");
    }

    [Fact]
    public void Create_WithEmptyKey_ShouldThrow()
    {
        var act = () => TenantSetting.Create(Guid.NewGuid(), "", "value");
        act.Should().Throw<ArgumentException>().WithMessage("*anahtarı*boş*");
    }

    [Fact]
    public void UpdateValue_ShouldChange()
    {
        var setting = TenantSetting.Create(Guid.NewGuid(), "Key", "old");

        setting.UpdateValue("new");

        setting.Value.Should().Be("new");
    }
}
