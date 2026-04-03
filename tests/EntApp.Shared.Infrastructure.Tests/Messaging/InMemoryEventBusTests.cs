using EntApp.Shared.Contracts.Events;
using EntApp.Shared.Contracts.Messaging;
using EntApp.Shared.Infrastructure.Messaging;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace EntApp.Shared.Infrastructure.Tests.Messaging;

public sealed record TestIntegrationEvent(string OrderId) : IntegrationEvent;

public class InMemoryEventBusTests
{
    private readonly IMediator _mediator;
    private readonly IEventBus _eventBus;

    public InMemoryEventBusTests()
    {
        _mediator = Substitute.For<IMediator>();
        _eventBus = new InMemoryEventBus(_mediator);
    }

    [Fact]
    public async Task PublishAsync_ShouldCallMediatorPublish()
    {
        var @event = new TestIntegrationEvent("ORD-001");

        await _eventBus.PublishAsync(@event);

        await _mediator.Received(1).Publish(
            Arg.Is<TestIntegrationEvent>(e => e.OrderId == "ORD-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_NullEvent_ShouldThrow()
    {
        var act = async () => await _eventBus.PublishAsync<TestIntegrationEvent>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void IntegrationEvent_ShouldAutoGenerateFields()
    {
        var @event = new TestIntegrationEvent("ORD-002");

        @event.Id.Should().NotBe(Guid.Empty);
        @event.IdempotencyKey.Should().NotBe(Guid.Empty);
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void IntegrationEvent_TwoInstances_ShouldHaveDifferentIds()
    {
        var event1 = new TestIntegrationEvent("ORD-001");
        var event2 = new TestIntegrationEvent("ORD-001");

        event1.Id.Should().NotBe(event2.Id);
        event1.IdempotencyKey.Should().NotBe(event2.IdempotencyKey);
    }
}
