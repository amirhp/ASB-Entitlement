using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ASB.Entitlements.Application.Queries.GetIdentity;

public sealed class GetIdentityByIdQueryHandler
    : IRequestHandler<GetIdentityByIdQuery, Result<Identity>>
{
    private readonly IIdentityRepository _repository;
    private readonly ILogger<GetIdentityByIdQueryHandler> _logger;

    public GetIdentityByIdQueryHandler(
        IIdentityRepository repository,
        ILogger<GetIdentityByIdQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Identity>> Handle(
        GetIdentityByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving identity with ID: {Id}", request.Id);

        try
        {
            var result = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Identity retrieved successfully: {Id}", request.Id);
            }
            else
            {
                _logger.LogWarning("Identity not found: {Id}", request.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving identity with ID: {Id}", request.Id);
            return Result.Failure<Identity>("An unexpected error occurred while retrieving the identity");
        }
    }
}
