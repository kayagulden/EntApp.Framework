using EntApp.Shared.Kernel.Domain;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Kernel.Tests.Domain;

public readonly record struct CustomerId(Guid Value) : IEntityId;
public readonly record struct OrderId(Guid Value) : IEntityId;

public class EntityIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueIds()
    {
        var id1 = EntityId.New<CustomerId>();
        var id2 = EntityId.New<CustomerId>();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void From_ShouldWrapGuid()
    {
        var guid = Guid.NewGuid();

        var customerId = EntityId.From<CustomerId>(guid);

        customerId.Value.Should().Be(guid);
    }

    [Fact]
    public void From_EmptyGuid_ShouldThrow()
    {
        var act = () => EntityId.From<CustomerId>(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DifferentTypes_SameGuid_ShouldNotBeEqual()
    {
        var guid = Guid.NewGuid();

        var customerId = EntityId.From<CustomerId>(guid);
        var orderId = EntityId.From<OrderId>(guid);

        // Farklı tipler — compile-time'da zaten karşılaştırılamaz (record struct)
        // Runtime'da boxing ile karşılaştırdığımızda false dönmeli
        customerId.Equals(orderId).Should().BeFalse();
    }

    [Fact]
    public void SameType_SameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();

        var id1 = EntityId.From<CustomerId>(guid);
        var id2 = EntityId.From<CustomerId>(guid);

        id1.Should().Be(id2);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var id = EntityId.From<CustomerId>(guid);

        id.ToString().Should().Contain(guid.ToString());
    }
}
