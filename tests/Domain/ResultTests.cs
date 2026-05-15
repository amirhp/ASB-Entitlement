using System;
using ASB.Entitlements.Domain.Common;
using FluentAssertions;
using Xunit;

namespace ASB.Entitlements.Tests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void Failure_CreatesFailureResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var result = Result.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void GenericSuccess_CreatesSuccessResultWithValue()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void GenericFailure_CreatesFailureResultWithError()
    {
        // Arrange
        var errorMessage = "Operation failed";

        // Act
        var result = Result.Failure<string>(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ThrowsException()
    {
        // Arrange
        var result = Result.Failure<string>("Error");

        // Act
        Action act = () => { var value = result.Value; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*failure result*");
    }

    [Fact]
    public void GenericSuccess_WithComplexObject_StoresCorrectly()
    {
        // Arrange
        var complexObject = new { Id = 1, Name = "Test" };

        // Act
        var result = Result.Success(complexObject);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Name.Should().Be("Test");
    }

    [Fact]
    public void IsSuccess_IsOppositeOfIsFailure()
    {
        // Arrange & Act
        var successResult = Result.Success();
        var failureResult = Result.Failure("Error");

        // Assert
        successResult.IsSuccess.Should().Be(!successResult.IsFailure);
        failureResult.IsSuccess.Should().Be(!failureResult.IsFailure);
    }

    [Theory]
    [InlineData("Error message 1")]
    [InlineData("Database connection failed")]
    [InlineData("Validation error")]
    public void Failure_PreservesErrorMessage(string errorMessage)
    {
        // Act
        var result = Result.Failure(errorMessage);

        // Assert
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public void GenericSuccess_WithNull_ShouldStoreNull()
    {
        // Act
        var result = Result.Success<string?>(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
