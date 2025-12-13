using Common.Domain.ValueObjects;
using FluentAssertions;

namespace Common.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_ValidAmount_CreatesMoney()
    {
        // Arrange & Act
        var money = new Money(100.50m, "BRL");

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Constructor_DefaultCurrency_UsesBRL()
    {
        // Arrange & Act
        var money = new Money(50m);

        // Assert
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new Money(-10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*não pode ser negativo*");
    }

    [Fact]
    public void Constructor_EmptyCurrency_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new Money(10m, "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*não pode ser vazia*");
    }

    [Fact]
    public void Constructor_RoundsToTwoDecimalPlaces()
    {
        // Arrange & Act
        var money = new Money(10.555m);

        // Assert
        money.Amount.Should().Be(10.56m);
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        // Arrange
        var money1 = new Money(100m, "BRL");
        var money2 = new Money(50m, "BRL");

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100m, "BRL");
        var money2 = new Money(50m, "USD");

        // Act
        var act = () => money1.Add(money2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*moedas diferentes*");
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        // Arrange
        var money1 = new Money(100m, "BRL");
        var money2 = new Money(30m, "BRL");

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Multiply_ReturnsProduct()
    {
        // Arrange
        var money = new Money(100m, "BRL");

        // Act
        var result = money.Multiply(2.5m);

        // Assert
        result.Amount.Should().Be(250m);
    }

    [Fact]
    public void Divide_ReturnsQuotient()
    {
        // Arrange
        var money = new Money(100m, "BRL");

        // Act
        var result = money.Divide(4m);

        // Assert
        result.Amount.Should().Be(25m);
    }

    [Fact]
    public void Divide_ByZero_ThrowsDivideByZeroException()
    {
        // Arrange
        var money = new Money(100m, "BRL");

        // Act
        var act = () => money.Divide(0m);

        // Assert
        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void IsZero_WhenZero_ReturnsTrue()
    {
        // Arrange
        var money = Money.Zero();

        // Act & Assert
        money.IsZero().Should().BeTrue();
    }

    [Fact]
    public void IsPositive_WhenPositive_ReturnsTrue()
    {
        // Arrange
        var money = new Money(100m);

        // Act & Assert
        money.IsPositive().Should().BeTrue();
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        // Arrange
        var money1 = new Money(100m, "BRL");
        var money2 = new Money(100m, "BRL");

        // Act & Assert
        money1.Should().Be(money2);
    }

    [Fact]
    public void Equals_DifferentAmount_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100m, "BRL");
        var money2 = new Money(50m, "BRL");

        // Act & Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var money = new Money(1234.56m, "BRL");

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Contain("BRL");
        result.Should().Contain("1");
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ReturnsAmount()
    {
        // Arrange
        var money = new Money(100.50m, "BRL");

        // Act
        decimal amount = money;

        // Assert
        amount.Should().Be(100.50m);
    }
}
