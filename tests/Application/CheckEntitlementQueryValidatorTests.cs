using ASB.Entitlements.Application.Queries.CheckEntitlement;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace ASB.Entitlements.Tests.Application;

public class CheckEntitlementQueryValidatorTests
{
    private readonly CheckEntitlementQueryValidator _validator;

    public CheckEntitlementQueryValidatorTests()
    {
        _validator = new CheckEntitlementQueryValidator();
    }

    [Fact]
    public void Validate_WithValidQuery_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "resource1", "ViewAccount");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyIdentityId_ShouldHaveValidationError(string identityId)
    {
        // Arrange
        var query = new CheckEntitlementQuery(identityId, "resource1", "ViewAccount");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdentityId)
            .WithErrorMessage("Identity ID is required");
    }

    [Fact]
    public void Validate_WithIdentityIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longIdentityId = new string('a', 101); // 101 characters
        var query = new CheckEntitlementQuery(longIdentityId, "resource1", "ViewAccount");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdentityId)
            .WithErrorMessage("Identity ID cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyResourceId_ShouldHaveValidationError(string resourceId)
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", resourceId, "ViewAccount");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResourceId)
            .WithErrorMessage("Resource ID is required");
    }

    [Fact]
    public void Validate_WithResourceIdTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longResourceId = new string('a', 101); // 101 characters
        var query = new CheckEntitlementQuery("user1", longResourceId, "ViewAccount");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResourceId)
            .WithErrorMessage("Resource ID cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyAction_ShouldHaveValidationError(string action)
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "resource1", action);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Action)
            .WithErrorMessage("Action is required");
    }

    [Theory]
    [InlineData("View123")]
    [InlineData("Edit_Account")]
    [InlineData("Delete-Resource")]
    [InlineData("View Account")]
    public void Validate_WithInvalidActionFormat_ShouldHaveValidationError(string action)
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "resource1", action);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Action)
            .WithErrorMessage("Action must contain only letters");
    }

    [Theory]
    [InlineData("View")]
    [InlineData("ViewAccount")]
    [InlineData("EDIT")]
    [InlineData("delete")]
    public void Validate_WithValidActionFormat_ShouldNotHaveValidationError(string action)
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "resource1", action);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    public void Validate_WithAllInvalidFields_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var query = new CheckEntitlementQuery("", "", "View123");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.Errors.Should().HaveCount(3);
        result.ShouldHaveValidationErrorFor(x => x.IdentityId);
        result.ShouldHaveValidationErrorFor(x => x.ResourceId);
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }
}
