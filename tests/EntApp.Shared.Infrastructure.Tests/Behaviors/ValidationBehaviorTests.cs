using EntApp.Shared.Infrastructure.Behaviors;
using EntApp.Shared.Kernel.Results;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;
using Xunit;

namespace EntApp.Shared.Infrastructure.Tests.Behaviors;

// Test request
public sealed record CreateUserCommand(string Name, string Email) : IRequest<Result<int>>;

// Test validator
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Email).EmailAddress().WithMessage("Invalid email.");
    }
}

public class ValidationBehaviorTests
{
    [Fact]
    public async Task ValidRequest_ShouldPassThrough()
    {
        var validators = new[] { new CreateUserCommandValidator() };
        var sut = new ValidationBehavior<CreateUserCommand, Result<int>>(validators);

        var request = new CreateUserCommand("John", "john@test.com");
        var next = Substitute.For<RequestHandlerDelegate<Result<int>>>();
        next().Returns(Task.FromResult(Result<int>.Success(42)));

        var result = await sut.Handle(request, next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task InvalidRequest_ShouldReturnValidationFailure()
    {
        var validators = new[] { new CreateUserCommandValidator() };
        var sut = new ValidationBehavior<CreateUserCommand, Result<int>>(validators);

        var request = new CreateUserCommand("", "not-an-email");
        var next = Substitute.For<RequestHandlerDelegate<Result<int>>>();
        next().Returns(Task.FromResult(Result<int>.Success(42)));

        var result = await sut.Handle(request, next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        await next.DidNotReceive().Invoke(); // Handler'a ulaşmamalı (validation'dan 2 kez çağrılıyor olabilir)
    }

    [Fact]
    public async Task NoValidators_ShouldPassThrough()
    {
        var validators = Enumerable.Empty<IValidator<CreateUserCommand>>();
        var sut = new ValidationBehavior<CreateUserCommand, Result<int>>(validators);

        var request = new CreateUserCommand("", "");
        var next = Substitute.For<RequestHandlerDelegate<Result<int>>>();
        next().Returns(Task.FromResult(Result<int>.Success(1)));

        var result = await sut.Handle(request, next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
