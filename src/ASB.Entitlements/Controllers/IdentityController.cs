using ASB.Entitlements.Application.Commands.CreateIdentity;
using ASB.Entitlements.Application.Queries.GetIdentity;
using ASB.Entitlements.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ASB.Entitlements.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IMediator mediator,
        ILogger<IdentityController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new identity (user, customer, service, or system)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IdentityResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIdentityRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating identity: {Id}", request.Id);

        var command = new CreateIdentityCommand(request.Id, request.Name, request.Type);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ErrorResponse { Error = result.Error });
        }

        var response = MapToResponse(result.Value);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, response);
    }

    /// <summary>
    /// Gets an identity by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IdentityResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving identity: {Id}", id);

        var query = new GetIdentityByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ErrorResponse { Error = result.Error });
        }

        var response = MapToResponse(result.Value);
        return Ok(response);
    }

    /// <summary>
    /// Maps domain Identity entity to API response
    /// This demonstrates proper separation of concerns - domain entities don't leak to API
    /// </summary>
    private IdentityResponse MapToResponse(Identity identity)
    {
        return new IdentityResponse
        {
            Id = identity.Id,
            Name = identity.Name,
            Type = identity.Type.ToString(),
            IsActive = identity.IsActive,
            CreatedAt = identity.CreatedAt
        };
    }

    public record CreateIdentityRequest(
        string Id,
        string Name,
        IdentityType Type);

    public record IdentityResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    public record ErrorResponse
    {
        public string Error { get; init; } = string.Empty;
    }
}
