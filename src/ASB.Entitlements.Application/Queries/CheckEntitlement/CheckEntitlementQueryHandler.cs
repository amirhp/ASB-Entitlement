using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ASB.Entitlements.Application.Queries.CheckEntitlement;

public sealed class CheckEntitlementQueryHandler
    : IRequestHandler<CheckEntitlementQuery, Result<EntitlementCheckResult>>
{
    private readonly IEntitlementRepository _repository;
    private readonly ILogger<CheckEntitlementQueryHandler> _logger;

    public CheckEntitlementQueryHandler(
        IEntitlementRepository repository,
        ILogger<CheckEntitlementQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<EntitlementCheckResult>> Handle(
        CheckEntitlementQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Checking entitlement for Identity: {IdentityId}, Resource: {ResourceId}, Action: {Action}",
            request.IdentityId,
            request.ResourceId,
            request.Action);

        try
        {
            var result = await _repository.CheckEntitlementAsync(
                request.IdentityId,
                request.ResourceId,
                request.Action,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Entitlement check completed. Identity: {IdentityId}, Entitled: {IsEntitled}",
                    request.IdentityId,
                    result.Value.IsEntitled);
            }
            else
            {
                _logger.LogWarning(
                    "Entitlement check failed. Identity: {IdentityId}, Error: {Error}",
                    request.IdentityId,
                    result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during entitlement check for Identity: {IdentityId}",
                request.IdentityId);

            return Result.Failure<EntitlementCheckResult>(
                "An unexpected error occurred while checking entitlement");
        }
    }
}
