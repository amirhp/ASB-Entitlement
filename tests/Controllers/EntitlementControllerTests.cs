using System;
using System.Threading;
using System.Threading.Tasks;
using ASB.Entitlements.Application.Queries.CheckEntitlement;
using ASB.Entitlements.Controllers;
using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static ASB.Entitlements.Controllers.EntitlementController;

namespace ASB.Entitlements.Tests.Controllers;

public class EntitlementControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<EntitlementController>> _mockLogger;
    private readonly EntitlementController _controller;

    public EntitlementControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<EntitlementController>>();
        _controller = new EntitlementController(_mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Check_WhenEntitlementIsGranted_ReturnsOkWithResponse()
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user1",
            ResourceId = "acc1",
            Action = "ViewAccount"
        };

        var entitlementResult = EntitlementCheckResult.Granted(
            "ViewAccount",
            "Access granted via role-based entitlement");

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(entitlementResult));

        // Act
        var result = await _controller.Check(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<EntitlementCheckResponse>().Subject;

        response.Entitled.Should().BeTrue();
        response.GrantedPermission.Should().Be("ViewAccount");
        response.Reason.Should().Be("Access granted via role-based entitlement");
        response.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Check_WhenEntitlementIsDenied_ReturnsOkWithDeniedResponse()
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user1",
            ResourceId = "acc2",
            Action = "EditAccount"
        };

        var entitlementResult = EntitlementCheckResult.Denied(
            "No matching permission found for the requested action");

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(entitlementResult));

        // Act
        var result = await _controller.Check(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<EntitlementCheckResponse>().Subject;

        response.Entitled.Should().BeFalse();
        response.GrantedPermission.Should().BeNull();
        response.Reason.Should().Be("No matching permission found for the requested action");
    }

    [Fact]
    public async Task Check_WhenMediatorReturnsFailure_ReturnsBadRequest()
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user1",
            ResourceId = "acc1",
            Action = "ViewAccount"
        };

        var errorMessage = "Database connection failed";
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EntitlementCheckResult>(errorMessage));

        // Act
        var result = await _controller.Check(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;

        errorResponse.Error.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Check_SendsCorrectQueryToMediator()
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user123",
            ResourceId = "resource456",
            Action = "CustomAction"
        };

        var entitlementResult = EntitlementCheckResult.Granted("CustomAction", "Success");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(entitlementResult));

        // Act
        await _controller.Check(request, CancellationToken.None);

        // Assert
        _mockMediator.Verify(
            m => m.Send(
                It.Is<CheckEntitlementQuery>(q =>
                    q.IdentityId == "user123" &&
                    q.ResourceId == "resource456" &&
                    q.Action == "CustomAction"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Check_LogsInformationRequest()
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user1",
            ResourceId = "acc1",
            Action = "ViewAccount"
        };

        var entitlementResult = EntitlementCheckResult.Granted("ViewAccount", "Success");
        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(entitlementResult));

        // Act
        await _controller.Check(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Entitlement check requested")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Check_LogsWarningOnFailure()
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user1",
            ResourceId = "acc1",
            Action = "ViewAccount"
        };

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EntitlementCheckResult>("Error occurred"));

        // Act
        await _controller.Check(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Entitlement check failed")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Check_PassesCancellationTokenToMediator()
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user1",
            ResourceId = "acc1",
            Action = "ViewAccount"
        };

        var cancellationToken = new CancellationToken();
        var entitlementResult = EntitlementCheckResult.Granted("ViewAccount", "Success");

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), cancellationToken))
            .ReturnsAsync(Result.Success(entitlementResult));

        // Act
        await _controller.Check(request, cancellationToken);

        // Assert
        _mockMediator.Verify(
            m => m.Send(It.IsAny<CheckEntitlementQuery>(), cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData("ViewAccount", true)]
    [InlineData("EditAccount", true)]
    [InlineData("DeleteAccount", true)]
    [InlineData("InvalidAction", false)]
    public async Task Check_HandlesVariousActions(string action, bool shouldSucceed)
    {
        // Arrange
        var request = new EntitlementCheckRequest
        {
            IdentityId = "user1",
            ResourceId = "acc1",
            Action = action
        };

        Result<EntitlementCheckResult> mediatorResult;
        if (shouldSucceed)
        {
            var entitlementResult = EntitlementCheckResult.Granted(action, "Success");
            mediatorResult = Result.Success(entitlementResult);
        }
        else
        {
            mediatorResult = Result.Failure<EntitlementCheckResult>("Validation failed");
        }

        _mockMediator
            .Setup(m => m.Send(It.IsAny<CheckEntitlementQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediatorResult);

        // Act
        var result = await _controller.Check(request, CancellationToken.None);

        // Assert
        if (shouldSucceed)
        {
            result.Should().BeOfType<OkObjectResult>();
        }
        else
        {
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
