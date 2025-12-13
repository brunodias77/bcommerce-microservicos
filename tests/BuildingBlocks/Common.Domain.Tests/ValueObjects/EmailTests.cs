using Common.Domain.ValueObjects;
using FluentAssertions;

namespace Common.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@subdomain.domain.com")]
    public void Constructor_ValidEmail_CreatesEmail(string validEmail)
    {
        // Arrange & Act
        var email = new Email(validEmail);

        // Assert
        email.Value.Should().Be(validEmail.ToLower());
    }

    [Fact]
    public void Constructor_NullEmail_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new Email(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*não pode ser vazio*");
    }

    [Fact]
    public void Constructor_EmptyEmail_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new Email("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*não pode ser vazio*");
    }

    [Fact]
    public void Constructor_WhitespaceEmail_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new Email("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*não pode ser vazio*");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@domain.com")]
    [InlineData("invalid@domain")]
    [InlineData("invalid @domain.com")]
    public void Constructor_InvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Arrange & Act
        var act = () => new Email(invalidEmail);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*inválido*");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange & Act
        var email = new Email("  test@example.com  ");

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Constructor_ConvertsToLowerCase()
    {
        // Arrange & Act
        var email = new Email("TEST@EXAMPLE.COM");

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void GetDomain_ReturnsEmailDomain()
    {
        // Arrange
        var email = new Email("user@example.com");

        // Act
        var domain = email.GetDomain();

        // Assert
        domain.Should().Be("example.com");
    }

    [Fact]
    public void GetLocalPart_ReturnsEmailLocalPart()
    {
        // Arrange
        var email = new Email("user@example.com");

        // Act
        var localPart = email.GetLocalPart();

        // Assert
        localPart.Should().Be("user");
    }

    [Fact]
    public void ToString_ReturnsEmailValue()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act
        string value = email;

        // Assert
        value.Should().Be("test@example.com");
    }

    [Fact]
    public void Equals_SameEmail_ReturnsTrue()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("TEST@EXAMPLE.COM");

        // Act & Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_DifferentEmail_ReturnsFalse()
    {
        // Arrange
        var email1 = new Email("test1@example.com");
        var email2 = new Email("test2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
    }

    [Fact]
    public void GetHashCode_SameEmail_ReturnsSameHash()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");

        // Act & Assert
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }
}
