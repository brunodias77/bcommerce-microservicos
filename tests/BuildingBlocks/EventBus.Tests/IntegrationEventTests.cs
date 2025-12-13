using EventBus.Abstractions;
using FluentAssertions;

namespace EventBus.Tests;

public class IntegrationEventTests
{
    private class TestIntegrationEvent : IntegrationEvent
    {
        public string TestProperty { get; }

        public TestIntegrationEvent(string testProperty) : base()
        {
            TestProperty = testProperty;
        }

        public TestIntegrationEvent(Guid id, DateTime occurredOn, string testProperty)
            : base(id, occurredOn)
        {
            TestProperty = testProperty;
        }
    }

    [Fact]
    public void Constructor_Default_GeneratesNewId()
    {
        // Arrange & Act
        var event1 = new TestIntegrationEvent("test1");
        var event2 = new TestIntegrationEvent("test2");

        // Assert
        event1.Id.Should().NotBe(Guid.Empty);
        event2.Id.Should().NotBe(Guid.Empty);
        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void Constructor_Default_SetsOccurredOnToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var @event = new TestIntegrationEvent("test");

        // Assert
        var after = DateTime.UtcNow;
        @event.OccurredOn.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_Default_SetsEventTypeToClassName()
    {
        // Arrange & Act
        var @event = new TestIntegrationEvent("test");

        // Assert
        @event.EventType.Should().Be(nameof(TestIntegrationEvent));
    }

    [Fact]
    public void Constructor_WithIdAndOccurredOn_SetsProvidedValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var occurredOn = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var @event = new TestIntegrationEvent(id, occurredOn, "test");

        // Assert
        @event.Id.Should().Be(id);
        @event.OccurredOn.Should().Be(occurredOn);
    }

    [Fact]
    public void Constructor_WithIdAndOccurredOn_StillSetsEventType()
    {
        // Arrange
        var id = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;

        // Act
        var @event = new TestIntegrationEvent(id, occurredOn, "test");

        // Assert
        @event.EventType.Should().Be(nameof(TestIntegrationEvent));
    }

    [Fact]
    public void DifferentEvents_HaveDifferentIds()
    {
        // Arrange & Act
        var events = Enumerable.Range(0, 100)
            .Select(i => new TestIntegrationEvent($"test{i}"))
            .ToList();

        // Assert
        var distinctIds = events.Select(e => e.Id).Distinct().Count();
        distinctIds.Should().Be(100);
    }
}
