using System;
using System.Threading;
using ASB.Entitlements.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ASB.Entitlements.Tests.Domain;

public class EntitlementCheckResultTests
{
    [Fact]
    public void Granted_CreatesGrantedResult()
    {
        // Arrange
        var permissionName = "ViewAccount";
        var reason = "Access granted via role-based entitlement";

        // Act
        var result = EntitlementCheckResult.Granted(permissionName, reason);

        // Assert
        result.IsEntitled.Should().BeTrue();
        result.GrantedPermission.Should().Be(permissionName);
        result.Reason.Should().Be(reason);
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Denied_CreatesDeniedResult()
    {
        // Arrange
        var reason = "No matching permission found";

        // Act
        var result = EntitlementCheckResult.Denied(reason);

        // Assert
        result.IsEntitled.Should().BeFalse();
        result.GrantedPermission.Should().BeNull();
        result.Reason.Should().Be(reason);
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("ViewAccount")]
    [InlineData("EditAccount")]
    [InlineData("DeleteAccount")]
    public void Granted_WithDifferentPermissions_StoresCorrectPermission(string permission)
    {
        // Act
        var result = EntitlementCheckResult.Granted(permission, "Success");

        // Assert
        result.GrantedPermission.Should().Be(permission);
    }

    [Theory]
    [InlineData("User not found")]
    [InlineData("Resource does not exist")]
    [InlineData("Permission denied by policy")]
    public void Denied_WithDifferentReasons_StoresCorrectReason(string reason)
    {
        // Act
        var result = EntitlementCheckResult.Denied(reason);

        // Assert
        result.Reason.Should().Be(reason);
    }

    [Fact]
    public void Granted_CheckedAtTimestamp_IsInUtc()
    {
        // Act
        var result = EntitlementCheckResult.Granted("ViewAccount", "Success");

        // Assert
        result.CheckedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Denied_CheckedAtTimestamp_IsInUtc()
    {
        // Act
        var result = EntitlementCheckResult.Denied("Failure");

        // Assert
        result.CheckedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void MultipleResults_HaveDifferentTimestamps()
    {
        // Act
        var result1 = EntitlementCheckResult.Granted("ViewAccount", "Success");
        Thread.Sleep(10); // Small delay
        var result2 = EntitlementCheckResult.Granted("EditAccount", "Success");

        // Assert
        result2.CheckedAt.Should().BeAfter(result1.CheckedAt);
    }

    [Fact]
    public void EntitlementCheckResult_IsImmutable()
    {
        // Arrange
        var result = EntitlementCheckResult.Granted("ViewAccount", "Success");

        // Assert - These properties should not have setters
        typeof(EntitlementCheckResult).GetProperty(nameof(EntitlementCheckResult.IsEntitled))!
            .SetMethod.Should().BeNull("IsEntitled should be read-only");

        typeof(EntitlementCheckResult).GetProperty(nameof(EntitlementCheckResult.Reason))!
            .SetMethod.Should().BeNull("Reason should be read-only");

        typeof(EntitlementCheckResult).GetProperty(nameof(EntitlementCheckResult.GrantedPermission))!
            .SetMethod.Should().BeNull("GrantedPermission should be read-only");

        typeof(EntitlementCheckResult).GetProperty(nameof(EntitlementCheckResult.CheckedAt))!
            .SetMethod.Should().BeNull("CheckedAt should be read-only");
    }
}
