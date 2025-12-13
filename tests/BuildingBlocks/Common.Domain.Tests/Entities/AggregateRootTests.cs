using Common.Domain.Entities;
using FluentAssertions;

namespace Common.Domain.Tests.Entities;

public class AggregateRootTests
{
    private class TestAggregateRoot : AggregateRoot
    {
        public TestAggregateRoot() : base() { }
    }

    [Fact]
    public void Constructor_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var aggregate = new TestAggregateRoot();

        // Assert
        var after = DateTime.UtcNow;
        aggregate.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_SetsUpdatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var aggregate = new TestAggregateRoot();

        // Assert
        var after = DateTime.UtcNow;
        aggregate.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_SetsVersionToOne()
    {
        // Arrange & Act
        var aggregate = new TestAggregateRoot();

        // Assert
        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void Constructor_DeletedAtIsNull()
    {
        // Arrange & Act
        var aggregate = new TestAggregateRoot();

        // Assert
        aggregate.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void IsDeleted_WhenNotDeleted_ReturnsFalse()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();

        // Act & Assert
        aggregate.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Delete_SetsDeletedAtToUtcNow()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        var before = DateTime.UtcNow;

        // Act
        aggregate.Delete();

        // Assert
        var after = DateTime.UtcNow;
        aggregate.DeletedAt.Should().NotBeNull();
        aggregate.DeletedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Delete_SetsIsDeletedToTrue()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();

        // Act
        aggregate.Delete();

        // Assert
        aggregate.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_UpdatesUpdatedAt()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        var originalUpdatedAt = aggregate.UpdatedAt;

        // Small delay to ensure time difference
        Thread.Sleep(10);

        // Act
        aggregate.Delete();

        // Assert
        aggregate.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        aggregate.Delete();

        // Act
        var act = () => aggregate.Delete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*já está excluída*");
    }

    [Fact]
    public void Restore_SetsDeletedAtToNull()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        aggregate.Delete();

        // Act
        aggregate.Restore();

        // Assert
        aggregate.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Restore_SetsIsDeletedToFalse()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        aggregate.Delete();

        // Act
        aggregate.Restore();

        // Assert
        aggregate.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_WhenNotDeleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();

        // Act
        var act = () => aggregate.Restore();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*não está excluída*");
    }

    [Fact]
    public void IncrementVersion_IncrementsVersion()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        var originalVersion = aggregate.Version;

        // Act
        aggregate.IncrementVersion();

        // Assert
        aggregate.Version.Should().Be(originalVersion + 1);
    }

    [Fact]
    public void IncrementVersion_UpdatesUpdatedAt()
    {
        // Arrange
        var aggregate = new TestAggregateRoot();
        var originalUpdatedAt = aggregate.UpdatedAt;

        Thread.Sleep(10);

        // Act
        aggregate.IncrementVersion();

        // Assert
        aggregate.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
