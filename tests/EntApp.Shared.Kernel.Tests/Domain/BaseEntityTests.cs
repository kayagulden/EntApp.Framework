using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Events;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Kernel.Tests.Domain;

// Test için concrete entity
public readonly record struct TestEntityId(Guid Value) : IEntityId;

public sealed class TestEntity : BaseEntity<TestEntityId>
{
    public TestEntity(TestEntityId id) : base(id) { }
    public TestEntity() { }
}

public class BaseEntityTests
{
    [Fact]
    public void Constructor_ShouldSetIdAndCreatedAt()
    {
        var id = EntityId.New<TestEntityId>();

        var entity = new TestEntity(id);

        entity.Id.Should().Be(id);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        entity.UpdatedAt.Should().BeNull();
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void RowVersion_ShouldHaveDefaultValue()
    {
        var entity = new TestEntity(EntityId.New<TestEntityId>());

        entity.RowVersion.Should().Be(0u);
    }

    [Fact]
    public void Equality_SameId_ShouldBeEqual()
    {
        var id = EntityId.New<TestEntityId>();

        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentId_ShouldNotBeEqual()
    {
        var entity1 = new TestEntity(EntityId.New<TestEntityId>());
        var entity2 = new TestEntity(EntityId.New<TestEntityId>());

        entity1.Should().NotBe(entity2);
        (entity1 != entity2).Should().BeTrue();
    }
}

// Test için concrete aggregate root
public readonly record struct TestAggregateId(Guid Value) : IEntityId;

public sealed class TestDomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string Message { get; init; } = string.Empty;
}

public sealed class TestAggregate : AggregateRoot<TestAggregateId>
{
    public TestAggregate(TestAggregateId id) : base(id) { }

    public void DoSomething(string message)
    {
        AddDomainEvent(new TestDomainEvent { Message = message });
    }
}

public class AggregateRootTests
{
    [Fact]
    public void AddDomainEvent_ShouldTrackEvent()
    {
        var aggregate = new TestAggregate(EntityId.New<TestAggregateId>());

        aggregate.DoSomething("test");

        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents[0].Should().BeOfType<TestDomainEvent>();
        ((TestDomainEvent)aggregate.DomainEvents[0]).Message.Should().Be("test");
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmpty()
    {
        var aggregate = new TestAggregate(EntityId.New<TestAggregateId>());
        aggregate.DoSomething("test1");
        aggregate.DoSomething("test2");

        aggregate.DomainEvents.Should().HaveCount(2);

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddDomainEvent_NullEvent_ShouldThrow()
    {
        var aggregate = new TestAggregate(EntityId.New<TestAggregateId>());

        var act = () => aggregate.AddDomainEvent(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AuditableEntity_ShouldHaveAuditFields()
    {
        var aggregate = new TestAggregate(EntityId.New<TestAggregateId>());

        aggregate.CreatedBy.Should().BeNull();
        aggregate.ModifiedBy.Should().BeNull();

        aggregate.CreatedBy = "admin";
        aggregate.ModifiedBy = "admin";

        aggregate.CreatedBy.Should().Be("admin");
        aggregate.ModifiedBy.Should().Be("admin");
    }
}
