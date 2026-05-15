using System;
using ASB.Entitlements.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ASB.Entitlements.Tests.Domain;

public class IdentityTests
{
    [Fact]
    public void Constructor_CreatesIdentityWithCorrectValues()
    {
        // Arrange & Act
        var identity = new Identity("user1", "Alice Johnson", IdentityType.Customer);

        // Assert
        identity.Id.Should().Be("user1");
        identity.Name.Should().Be("Alice Johnson");
        identity.Type.Should().Be(IdentityType.Customer);
        identity.IsActive.Should().BeTrue();
        identity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_WhenInactive_ShouldActivateIdentity()
    {
        // Arrange
        var identity = new Identity("user1", "Alice", IdentityType.Customer);
        identity.Deactivate();

        // Act
        identity.Activate();

        // Assert
        identity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateIdentity()
    {
        // Arrange
        var identity = new Identity("user1", "Alice", IdentityType.Customer);

        // Act
        identity.Deactivate();

        // Assert
        identity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var identity = new Identity("user1", "Alice", IdentityType.Customer);

        // Act
        identity.UpdateName("Alice Smith");

        // Assert
        identity.Name.Should().Be("Alice Smith");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void UpdateName_WithInvalidName_ShouldThrowException(string invalidName)
    {
        // Arrange
        var identity = new Identity("user1", "Alice", IdentityType.Customer);

        // Act
        Action act = () => identity.UpdateName(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Identity name cannot be null or empty*");
    }

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var identity1 = new Identity("user1", "Alice", IdentityType.Customer);
        var identity2 = new Identity("user1", "Bob", IdentityType.Service);

        // Act & Assert
        (identity1 == identity2).Should().BeTrue();
        identity1.Equals(identity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var identity1 = new Identity("user1", "Alice", IdentityType.Customer);
        var identity2 = new Identity("user2", "Alice", IdentityType.Customer);

        // Act & Assert
        (identity1 != identity2).Should().BeTrue();
        identity1.Equals(identity2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        var identity1 = new Identity("user1", "Alice", IdentityType.Customer);
        var identity2 = new Identity("user1", "Bob", IdentityType.Service);

        // Act & Assert
        identity1.GetHashCode().Should().Be(identity2.GetHashCode());
    }

    [Theory]
    [InlineData(IdentityType.Customer)]
    [InlineData(IdentityType.Employee)]
    [InlineData(IdentityType.Service)]
    [InlineData(IdentityType.System)]
    public void Constructor_WithDifferentTypes_ShouldCreateIdentityWithCorrectType(IdentityType type)
    {
        // Arrange & Act
        var identity = new Identity("id1", "Name", type);

        // Assert
        identity.Type.Should().Be(type);
    }

    [Fact]
    public void Identity_CanBeActivatedAndDeactivatedMultipleTimes()
    {
        // Arrange
        var identity = new Identity("user1", "Alice", IdentityType.Customer);

        // Act & Assert
        identity.IsActive.Should().BeTrue();

        identity.Deactivate();
        identity.IsActive.Should().BeFalse();

        identity.Activate();
        identity.IsActive.Should().BeTrue();

        identity.Deactivate();
        identity.IsActive.Should().BeFalse();
    }
}
