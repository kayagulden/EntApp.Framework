using EntApp.Shared.Infrastructure.DynamicCrud;
using EntApp.Shared.Infrastructure.DynamicCrud.Models;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace EntApp.Shared.Infrastructure.Tests.DynamicCrud;

public class MetadataServiceOverrideTests
{
    private readonly IMetadataService _sut;
    private readonly IDynamicUIConfigProvider _configProvider;
    private readonly IDynamicEntityRegistry _registry;

    public MetadataServiceOverrideTests()
    {
        _registry = BuildTestRegistry();
        _configProvider = Substitute.For<IDynamicUIConfigProvider>();
        _sut = new MetadataService(_registry, _configProvider);
    }

    // ─── Test 1: DB override yokken convention metadata döner ───

    [Fact]
    public async Task GetMetadataAsync_NoOverride_ReturnsConventionMetadata()
    {
        // Arrange
        _configProvider
            .GetOverrideAsync("TestEntity", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((DynamicUIConfigOverrideDto?)null);

        // Act
        var result = await _sut.GetMetadataAsync("TestEntity");

        // Assert
        result.Should().NotBeNull();
        result!.Entity.Should().Be("TestEntity");
        result.Title.Should().Be("Test Entity");
        result.Fields.Should().HaveCount(2);
        result.Fields[0].Name.Should().Be("code");
        result.Fields[1].Name.Should().Be("name");
        result.Fields[0].ShowInList.Should().BeTrue();
        result.Fields[0].Order.Should().Be(0);
        result.Fields[1].Order.Should().Be(1);
    }

    // ─── Test 2: DB override ile label, order, showInList merge edilir ───

    [Fact]
    public async Task GetMetadataAsync_WithOverride_MergesFieldProperties()
    {
        // Arrange
        var overrideDto = new DynamicUIConfigOverrideDto
        {
            Title = "Özel Başlık",
            Icon = "star",
            Fields = new Dictionary<string, FieldOverrideDto>
            {
                ["code"] = new FieldOverrideDto
                {
                    Label = "ISO Kodu",
                    Order = 10,
                    Width = "sm",
                    ShowInList = false
                },
                ["name"] = new FieldOverrideDto
                {
                    Label = "Ad",
                    Order = 5
                }
            }
        };

        _configProvider
            .GetOverrideAsync("TestEntity", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(overrideDto);

        // Act
        var result = await _sut.GetMetadataAsync("TestEntity");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Özel Başlık");
        result.Icon.Should().Be("star");

        // name → order 5, code → order 10 olmalı (sıralanmış)
        result.Fields[0].Name.Should().Be("name");
        result.Fields[0].Label.Should().Be("Ad");
        result.Fields[0].Order.Should().Be(5);

        result.Fields[1].Name.Should().Be("code");
        result.Fields[1].Label.Should().Be("ISO Kodu");
        result.Fields[1].ShowInList.Should().BeFalse();
        result.Fields[1].Width.Should().Be("sm");
        result.Fields[1].Order.Should().Be(10);
    }

    // ─── Test 3: Tenant override > Global override fallback ───

    [Fact]
    public async Task GetMetadataAsync_WithTenantId_PassesTenantToProvider()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _configProvider
            .GetOverrideAsync("TestEntity", tenantId, Arg.Any<CancellationToken>())
            .Returns(new DynamicUIConfigOverrideDto
            {
                Title = "Tenant Başlık"
            });

        // Act
        var result = await _sut.GetMetadataAsync("TestEntity", tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Tenant Başlık");

        await _configProvider.Received(1)
            .GetOverrideAsync("TestEntity", tenantId, Arg.Any<CancellationToken>());
    }

    // ─── Test 4: Field hidden=true metadata'dan çıkarılır ───

    [Fact]
    public async Task GetMetadataAsync_HiddenField_RemovedFromMetadata()
    {
        // Arrange
        var overrideDto = new DynamicUIConfigOverrideDto
        {
            Fields = new Dictionary<string, FieldOverrideDto>
            {
                ["code"] = new FieldOverrideDto { Hidden = true }
            }
        };

        _configProvider
            .GetOverrideAsync("TestEntity", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(overrideDto);

        // Act
        var result = await _sut.GetMetadataAsync("TestEntity");

        // Assert
        result.Should().NotBeNull();
        result!.Fields.Should().HaveCount(1);
        result.Fields[0].Name.Should().Be("name"); // code gizlendi
    }

    // ─── Test 5: Actions override çalışır ───

    [Fact]
    public async Task GetMetadataAsync_ActionOverride_MergesCorrectly()
    {
        // Arrange
        var overrideDto = new DynamicUIConfigOverrideDto
        {
            Actions = new ActionOverrideDto
            {
                Delete = false,
                Export = false
            }
        };

        _configProvider
            .GetOverrideAsync("TestEntity", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(overrideDto);

        // Act
        var result = await _sut.GetMetadataAsync("TestEntity");

        // Assert
        result.Should().NotBeNull();
        result!.Actions.Create.Should().BeTrue();  // override yok → default
        result.Actions.Edit.Should().BeTrue();      // override yok → default
        result.Actions.Delete.Should().BeFalse();   // override: false
        result.Actions.Export.Should().BeFalse();    // override: false
    }

    // ─── Test 6: Provider null iken async metadata base döner ───

    [Fact]
    public async Task GetMetadataAsync_NoProvider_ReturnsBaseMetadata()
    {
        // Arrange — provider olmadan MetadataService oluştur
        var sut = new MetadataService(_registry, configProvider: null);

        // Act
        var result = await sut.GetMetadataAsync("TestEntity");

        // Assert
        result.Should().NotBeNull();
        result!.Entity.Should().Be("TestEntity");
        result.Fields.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    private static IDynamicEntityRegistry BuildTestRegistry()
    {
        var registry = new DynamicEntityRegistry(NullLogger<DynamicEntityRegistry>.Instance);
        registry.ScanAssemblies(typeof(TestEntity).Assembly);
        return registry;
    }
}

// ── Test Entity ────────────────────────────────────────────

[DynamicEntity("TestEntity", MenuGroup = "Test")]
file class TestEntity : BaseEntity<Guid>
{
    [DynamicField(Required = true, MaxLength = 3)]
    public string Code { get; set; } = string.Empty;

    [DynamicField(Searchable = true)]
    public string Name { get; set; } = string.Empty;
}
