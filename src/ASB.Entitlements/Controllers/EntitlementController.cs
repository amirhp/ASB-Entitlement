using ASB.Entitlements.Application.Queries.CheckEntitlement;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ASB.Entitlements.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EntitlementController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EntitlementController> _logger;

    public EntitlementController(
        IMediator mediator,
        ILogger<EntitlementController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if an identity is entitled to perform an action on a resource
    /// </summary>
    /// <param name="request">Entitlement check request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entitlement check result</returns>
    [HttpPost("check")]
    [ProducesResponseType(typeof(EntitlementCheckResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Check(
        [FromBody] EntitlementCheckRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Entitlement check requested for Identity: {IdentityId}, Resource: {ResourceId}, Action: {Action}",
            request.IdentityId, request.ResourceId, request.Action);

        var query = new CheckEntitlementQuery(
            request.IdentityId,
            request.ResourceId,
            request.Action);

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Entitlement check failed: {Error}", result.Error);
            return BadRequest(new ErrorResponse { Error = result.Error });
        }

        var response = new EntitlementCheckResponse
        {
            Entitled = result.Value.IsEntitled,
            Reason = result.Value.Reason,
            GrantedPermission = result.Value.GrantedPermission,
            CheckedAt = result.Value.CheckedAt
        };

        _logger.LogDebug("Entitlement check completed: {Entitled}", response.Entitled);

        return Ok(response);
    }

    public class EntitlementCheckRequest
    {
        public string IdentityId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class EntitlementCheckResponse
    {
        public bool Entitled { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? GrantedPermission { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }
}
