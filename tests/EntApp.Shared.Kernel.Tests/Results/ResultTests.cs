using EntApp.Shared.Kernel.Results;
using FluentAssertions;
using Xunit;

namespace EntApp.Shared.Kernel.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccess()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldContainError()
    {
        var error = Error.NotFound("User.NotFound", "User was not found.");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void GenericResult_Success_ShouldHaveValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericResult_Failure_ShouldThrowOnValueAccess()
    {
        var error = Error.Validation("Invalid", "Value is invalid.");
        var result = Result<int>.Failure(error);

        result.IsFailure.Should().BeTrue();

        var act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidationFailure_ShouldContainMultipleErrors()
    {
        var errors = new[]
        {
            Error.Validation("Name.Required", "Name is required."),
            Error.Validation("Email.Invalid", "Email format is invalid.")
        };

        var result = Result<string>.ValidationFailure(errors);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Error.Should().Be(errors[0]); // İlk hata default error
    }

    [Fact]
    public void ImplicitOperator_ShouldConvertValueToSuccess()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void ImplicitOperator_NullValue_ShouldReturnFailure()
    {
        Result<string> result = (string?)null;

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Result.NullValue");
    }

    [Fact]
    public void Error_FactoryMethods_ShouldSetCorrectType()
    {
        Error.Validation("V", "msg").Type.Should().Be(ErrorType.Validation);
        Error.NotFound("N", "msg").Type.Should().Be(ErrorType.NotFound);
        Error.Conflict("C", "msg").Type.Should().Be(ErrorType.Conflict);
        Error.Unauthorized("U", "msg").Type.Should().Be(ErrorType.Unauthorized);
        Error.Failure("F", "msg").Type.Should().Be(ErrorType.Failure);
    }
}
