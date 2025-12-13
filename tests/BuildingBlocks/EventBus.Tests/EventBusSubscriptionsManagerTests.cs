using EventBus.Abstractions;
using FluentAssertions;

namespace EventBus.Tests;

public class EventBusSubscriptionsManagerTests
{
    private class TestIntegrationEvent : IntegrationEvent
    {
        public string Data { get; }
        public TestIntegrationEvent(string data) => Data = data;
    }

    private class AnotherIntegrationEvent : IntegrationEvent
    {
        public int Value { get; }
        public AnotherIntegrationEvent(int value) => Value = value;
    }

    private class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task HandleAsync(TestIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class AnotherTestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task HandleAsync(TestIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void IsEmpty_WhenNoSubscriptions_ReturnsTrue()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();

        // Act & Assert
        manager.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_AfterAddingSubscription_ReturnsFalse()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();

        // Act
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Assert
        manager.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void AddSubscription_AddsHandlerForEvent()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();

        // Act
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Assert
        manager.HasSubscriptionsForEvent<TestIntegrationEvent>().Should().BeTrue();
    }

    [Fact]
    public void AddSubscription_CanAddMultipleHandlersForSameEvent()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();

        // Act
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();
        manager.AddSubscription<TestIntegrationEvent, AnotherTestIntegrationEventHandler>();

        // Assert
        var handlers = manager.GetHandlersForEvent<TestIntegrationEvent>();
        handlers.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveSubscription_RemovesHandlerForEvent()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Act
        manager.RemoveSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Assert
        manager.HasSubscriptionsForEvent<TestIntegrationEvent>().Should().BeFalse();
    }

    [Fact]
    public void RemoveSubscription_OnlyRemovesSpecificHandler()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();
        manager.AddSubscription<TestIntegrationEvent, AnotherTestIntegrationEventHandler>();

        // Act
        manager.RemoveSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Assert
        manager.HasSubscriptionsForEvent<TestIntegrationEvent>().Should().BeTrue();
        var handlers = manager.GetHandlersForEvent<TestIntegrationEvent>();
        handlers.Should().HaveCount(1);
        handlers.Should().Contain(typeof(AnotherTestIntegrationEventHandler));
    }

    [Fact]
    public void GetEventKey_ReturnsEventTypeName()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();

        // Act
        var key = manager.GetEventKey<TestIntegrationEvent>();

        // Assert
        key.Should().Be(nameof(TestIntegrationEvent));
    }

    [Fact]
    public void GetEventTypeByName_ReturnsCorrectType()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Act
        var eventType = manager.GetEventTypeByName(nameof(TestIntegrationEvent));

        // Assert
        eventType.Should().Be(typeof(TestIntegrationEvent));
    }

    [Fact]
    public void HasSubscriptionsForEvent_WhenNoSubscription_ReturnsFalse()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();

        // Act & Assert
        manager.HasSubscriptionsForEvent<TestIntegrationEvent>().Should().BeFalse();
    }

    [Fact]
    public void HasSubscriptionsForEvent_ByName_WhenNoSubscription_ReturnsFalse()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();

        // Act & Assert
        manager.HasSubscriptionsForEvent(nameof(TestIntegrationEvent)).Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllSubscriptions()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();
        manager.AddSubscription<AnotherIntegrationEvent, AnotherIntegrationEventHandler>();

        // Act
        manager.Clear();

        // Assert
        manager.IsEmpty.Should().BeTrue();
    }

    private class AnotherIntegrationEventHandler : IIntegrationEventHandler<AnotherIntegrationEvent>
    {
        public Task HandleAsync(AnotherIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void OnEventRemoved_RaisesEventWhenLastHandlerRemoved()
    {
        // Arrange
        var manager = new EventBusSubscriptionsManager();
        manager.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();
        string? removedEventName = null;
        manager.OnEventRemoved += (sender, eventName) => removedEventName = eventName;

        // Act
        manager.RemoveSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Assert
        removedEventName.Should().Be(nameof(TestIntegrationEvent));
    }
}
