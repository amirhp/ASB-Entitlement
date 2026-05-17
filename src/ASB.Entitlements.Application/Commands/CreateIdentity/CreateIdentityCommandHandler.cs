using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ASB.Entitlements.Application.Commands.CreateIdentity;

public sealed class CreateIdentityCommandHandler
    : IRequestHandler<CreateIdentityCommand, Result<Identity>>
{
    private readonly IIdentityRepository _repository;
    private readonly ILogger<CreateIdentityCommandHandler> _logger;

    public CreateIdentityCommandHandler(
        IIdentityRepository repository,
        ILogger<CreateIdentityCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Identity>> Handle(
        CreateIdentityCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating identity with ID: {Id}, Name: {Name}, Type: {Type}",
            request.Id, request.Name, request.Type);

        try
        {
            // Check if identity already exists
            var existsResult = await _repository.ExistsAsync(request.Id, cancellationToken);
            if (existsResult.IsFailure)
                return Result.Failure<Identity>(existsResult.Error);

            if (existsResult.Value)
            {
                _logger.LogWarning("Identity with ID {Id} already exists", request.Id);
                return Result.Failure<Identity>($"Identity with ID '{request.Id}' already exists");
            }

            // Create domain entity - this is where business logic lives
            var identity = new Identity(request.Id, request.Name, request.Type);

            // Persist through repository
            var result = await _repository.CreateAsync(identity, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Identity created successfully: {Id}", request.Id);
            }
            else
            {
                _logger.LogError("Failed to create identity: {Error}", result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating identity with ID: {Id}", request.Id);
            return Result.Failure<Identity>("An unexpected error occurred while creating the identity");
        }
    }
}
