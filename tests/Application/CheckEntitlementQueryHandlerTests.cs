using System;
using System.Threading;
using System.Threading.Tasks;
using ASB.Entitlements.Application.Queries.CheckEntitlement;
using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ASB.Entitlements.Tests.Application;

public class CheckEntitlementQueryHandlerTests
{
    private readonly Mock<IEntitlementRepository> _mockRepository;
    private readonly Mock<ILogger<CheckEntitlementQueryHandler>> _mockLogger;
    private readonly CheckEntitlementQueryHandler _handler;

    public CheckEntitlementQueryHandlerTests()
    {
        _mockRepository = new Mock<IEntitlementRepository>();
        _mockLogger = new Mock<ILogger<CheckEntitlementQueryHandler>>();
        _handler = new CheckEntitlementQueryHandler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenUserIsEntitled_ReturnsSuccessResultWithGrantedAccess()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "acc1", "ViewAccount");
        var expectedResult = EntitlementCheckResult.Granted(
            "ViewAccount",
            "Access granted via role-based entitlement");

        _mockRepository
            .Setup(r => r.CheckEntitlementAsync("user1", "acc1", "ViewAccount", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEntitled.Should().BeTrue();
        result.Value.GrantedPermission.Should().Be("ViewAccount");
        result.Value.Reason.Should().Be("Access granted via role-based entitlement");
    }

    [Fact]
    public async Task Handle_WhenUserIsNotEntitled_ReturnsSuccessResultWithDeniedAccess()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "acc2", "EditAccount");
        var expectedResult = EntitlementCheckResult.Denied("No matching permission found for the requested action");

        _mockRepository
            .Setup(r => r.CheckEntitlementAsync("user1", "acc2", "EditAccount", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEntitled.Should().BeFalse();
        result.Value.GrantedPermission.Should().BeNull();
        result.Value.Reason.Should().Be("No matching permission found for the requested action");
    }

    [Fact]
    public async Task Handle_WhenRepositoryFails_ReturnsFailureResult()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "acc1", "ViewAccount");
        var errorMessage = "Database connection failed";

        _mockRepository
            .Setup(r => r.CheckEntitlementAsync("user1", "acc1", "ViewAccount", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EntitlementCheckResult>(errorMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user123", "resource456", "CustomAction");
        var expectedResult = EntitlementCheckResult.Granted("CustomAction", "Success");

        _mockRepository
            .Setup(r => r.CheckEntitlementAsync("user123", "resource456", "CustomAction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.CheckEntitlementAsync("user123", "resource456", "CustomAction", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsEntitlementCheck()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "acc1", "ViewAccount");
        var expectedResult = EntitlementCheckResult.Granted("ViewAccount", "Success");

        _mockRepository
            .Setup(r => r.CheckEntitlementAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Checking entitlement for")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "acc1", "ViewAccount");
        var cancellationToken = new CancellationToken();
        var expectedResult = EntitlementCheckResult.Granted("ViewAccount", "Success");

        _mockRepository
            .Setup(r => r.CheckEntitlementAsync("user1", "acc1", "ViewAccount", cancellationToken))
            .ReturnsAsync(Result.Success(expectedResult));

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockRepository.Verify(
            r => r.CheckEntitlementAsync("user1", "acc1", "ViewAccount", cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PreservesResultTimestamp()
    {
        // Arrange
        var query = new CheckEntitlementQuery("user1", "acc1", "ViewAccount");
        var now = DateTime.UtcNow;
        var expectedResult = EntitlementCheckResult.Granted("ViewAccount", "Success");

        _mockRepository
            .Setup(r => r.CheckEntitlementAsync("user1", "acc1", "ViewAccount", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedResult));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.CheckedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }
}
