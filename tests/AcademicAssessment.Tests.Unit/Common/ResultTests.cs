using AcademicAssessment.Core.Common;
using FluentAssertions;

namespace AcademicAssessment.Tests.Unit.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        // Arrange & Act
        var result = new Result<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Should().BeOfType<Result<int>.Success>();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        // Arrange
        var error = new Error("TEST_ERROR", "Test error message");

        // Act
        var result = new Result<int>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Result<int>.Failure>();
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        // Act
        Result<string> result = "Hello, World!";

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().BeOfType<Result<string>.Success>();
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFailure()
    {
        // Arrange
        var error = new Error("TEST", "Test");

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Result<int>.Failure>();
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        // Arrange
        var result = new Result<int>.Success(21);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Should().BeOfType<Result<int>.Success>();
        var success = (Result<int>.Success)mapped;
        success.Value.Should().Be(42);
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = new Result<int>.Failure(error);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsFailure.Should().BeTrue();
        var failure = (Result<int>.Failure)mapped;
        failure.Error.Code.Should().Be("TEST");
    }

    [Fact]
    public void Bind_OnSuccess_ShouldChainOperation()
    {
        // Arrange
        var result = new Result<int>.Success(10);

        // Act
        var bound = result.Bind(x => new Result<int>.Success(x + 5));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        var success = (Result<int>.Success)bound;
        success.Value.Should().Be(15);
    }

    [Fact]
    public void Bind_OnSuccess_ReturningFailure_ShouldReturnFailure()
    {
        // Arrange
        var result = new Result<int>.Success(10);
        var error = new Error("VALIDATION", "Too large");

        // Act
        var bound = result.Bind<int, int>(x => x > 5
            ? new Result<int>.Failure(error)
            : new Result<int>.Success(x));

        // Assert
        bound.IsFailure.Should().BeTrue();
        var failure = (Result<int>.Failure)bound;
        failure.Error.Code.Should().Be("VALIDATION");
    }

    [Fact]
    public void Bind_OnFailure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = new Result<int>.Failure(error);

        // Act
        var bound = result.Bind(x => new Result<int>.Success(x * 2));

        // Assert
        bound.IsFailure.Should().BeTrue();
        var failure = (Result<int>.Failure)bound;
        failure.Error.Code.Should().Be("TEST");
    }

    [Fact]
    public void Match_OnSuccess_ShouldExecuteSuccessFunction()
    {
        // Arrange
        var result = new Result<int>.Success(42);

        // Act
        var matched = result.Match(
            onSuccess: value => $"Value: {value}",
            onFailure: error => $"Error: {error.Message}");

        // Assert
        matched.Should().Be("Value: 42");
    }

    [Fact]
    public void Match_OnFailure_ShouldExecuteFailureFunction()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = new Result<int>.Failure(error);

        // Act
        var matched = result.Match(
            onSuccess: value => $"Value: {value}",
            onFailure: error => $"Error: {error.Message}");

        // Assert
        matched.Should().Be("Error: Test error");
    }

    [Fact]
    public void Tap_OnSuccess_ShouldExecuteAction()
    {
        // Arrange
        var result = new Result<int>.Success(42);
        var sideEffect = 0;

        // Act
        var tapped = result.Tap(value => sideEffect = value);

        // Assert
        tapped.IsSuccess.Should().BeTrue();
        sideEffect.Should().Be(42);
    }

    [Fact]
    public void Tap_OnFailure_ShouldNotExecuteAction()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = new Result<int>.Failure(error);
        var sideEffect = 0;

        // Act
        var tapped = result.Tap(value => sideEffect = value);

        // Assert
        tapped.IsFailure.Should().BeTrue();
        sideEffect.Should().Be(0);
    }

    [Fact]
    public void TapError_OnFailure_ShouldExecuteAction()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = new Result<int>.Failure(error);
        string? capturedCode = null;

        // Act
        var tapped = result.TapError(err => capturedCode = err.Code);

        // Assert
        tapped.IsFailure.Should().BeTrue();
        capturedCode.Should().Be("TEST");
    }

    [Fact]
    public void GetValueOrDefault_OnSuccess_ShouldReturnValue()
    {
        // Arrange
        var result = new Result<int>.Success(42);

        // Act
        var value = result.GetValueOrDefault(0);

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ShouldReturnDefault()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var result = new Result<int>.Failure(error);

        // Act
        var value = result.GetValueOrDefault(99);

        // Assert
        value.Should().Be(99);
    }

    [Fact]
    public void GetValueOrThrow_OnSuccess_ShouldReturnValue()
    {
        // Arrange
        var result = new Result<int>.Success(42);

        // Act
        var value = result.GetValueOrThrow();

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void GetValueOrThrow_OnFailure_ShouldThrowException()
    {
        // Arrange
        var error = new Error("TEST", "Test error");
        var result = new Result<int>.Failure(error);

        // Act & Assert
        var act = () => result.GetValueOrThrow();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Test error");
    }

    [Fact]
    public void Sequence_WithAllSuccess_ShouldReturnSuccessWithList()
    {
        // Arrange
        var results = new[]
        {
            new Result<int>.Success(1),
            new Result<int>.Success(2),
            new Result<int>.Success(3)
        };

        // Act
        var sequenced = results.Sequence();

        // Assert
        sequenced.IsSuccess.Should().BeTrue();
        var success = (Result<IReadOnlyList<int>>.Success)sequenced;
        success.Value.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Sequence_WithAnyFailure_ShouldReturnFirstFailure()
    {
        // Arrange
        var error = new Error("TEST", "Test");
        var results = new Result<int>[]
        {
            new Result<int>.Success(1),
            new Result<int>.Failure(error),
            new Result<int>.Success(3)
        };

        // Act
        var sequenced = results.Sequence();

        // Assert
        sequenced.IsFailure.Should().BeTrue();
        var failure = (Result<IReadOnlyList<int>>.Failure)sequenced;
        failure.Error.Code.Should().Be("TEST");
    }

    [Fact]
    public void Ensure_WhenPredicateTrue_ShouldReturnSuccess()
    {
        // Arrange
        var result = new Result<int>.Success(10);
        var error = new Error("VALIDATION", "Value too small");

        // Act
        var ensured = result.Ensure(x => x > 5, error);

        // Assert
        ensured.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Ensure_WhenPredicateFalse_ShouldReturnError()
    {
        // Arrange
        var result = new Result<int>.Success(3);
        var error = new Error("VALIDATION", "Value too small");

        // Act
        var ensured = result.Ensure(x => x > 5, error);

        // Assert
        ensured.IsFailure.Should().BeTrue();
        var failure = (Result<int>.Failure)ensured;
        failure.Error.Code.Should().Be("VALIDATION");
    }

    [Fact]
    public void RailwayOrientedProgramming_SuccessPath_ShouldFlowThrough()
    {
        // Arrange
        var input = "42";

        // Act - Railway-oriented pipeline
        var result = ParseInt(input)
            .Map(x => x * 2)
            .Bind<int, int>(x => x > 100 ? Error.Validation("Too large") : x)
            .Tap(x => Console.WriteLine($"Value: {x}"))
            .Match(
                onSuccess: x => $"Final: {x}",
                onFailure: e => $"Error: {e.Message}");

        // Assert
        result.Should().Be("Final: 84");
    }

    [Fact]
    public void RailwayOrientedProgramming_FailurePath_ShouldShortCircuit()
    {
        // Arrange
        var input = "invalid";

        // Act - Railway-oriented pipeline
        var result = ParseInt(input)
            .Map(x => x * 2)  // Should not execute
            .Bind<int, int>(x => x > 100 ? Error.Validation("Too large") : x)  // Should not execute
            .Match(
                onSuccess: x => $"Final: {x}",
                onFailure: e => $"Error: {e.Message}");

        // Assert
        result.Should().StartWith("Error:");
    }

    // Helper method for testing
    private static Result<int> ParseInt(string value) =>
        int.TryParse(value, out var result)
            ? new Result<int>.Success(result)
            : Error.Validation($"'{value}' is not a valid integer");
}
