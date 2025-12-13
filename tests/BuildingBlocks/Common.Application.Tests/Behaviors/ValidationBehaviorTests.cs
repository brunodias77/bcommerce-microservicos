using Common.Application.Behaviors;
using Common.Application.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace Common.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private record TestRequest(string Name, int Age) : IRequest<TestResponse>;
    private record TestResponse(bool Success);

    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nome é obrigatório");
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Idade deve ser maior que zero");
        }
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test", 25);
        var expectedResponse = new TestResponse(true);
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test", 25);
        var expectedResponse = new TestResponse(true);
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("", 0);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse(true));

        // Act
        var act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("Name");
        exception.Which.Errors.Should().ContainKey("Age");
    }

    [Fact]
    public async Task Handle_InvalidRequest_DoesNotCallNext()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("", 0);
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse(true));
        };

        // Act
        try
        {
            await behavior.Handle(request, next, CancellationToken.None);
        }
        catch (ValidationException)
        {
            // Expected
        }

        // Assert
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MultipleValidators_CombinesErrors()
    {
        // Arrange
        var validator1 = new Mock<IValidator<TestRequest>>();
        var validator2 = new Mock<IValidator<TestRequest>>();

        validator1.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Field1", "Error1") }));

        validator2.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Field2", "Error2") }));

        var validators = new List<IValidator<TestRequest>> { validator1.Object, validator2.Object };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("Test", 25);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse(true));

        // Act
        var act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey("Field1");
        exception.Which.Errors.Should().ContainKey("Field2");
    }
}
