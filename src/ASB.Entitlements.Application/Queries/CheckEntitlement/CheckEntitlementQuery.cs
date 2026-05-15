using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.ValueObjects;
using MediatR;

namespace ASB.Entitlements.Application.Queries.CheckEntitlement;

/// <summary>
/// Query to check if an identity is entitled to perform an action on a resource
/// </summary>
public sealed record CheckEntitlementQuery(
    string IdentityId,
    string ResourceId,
    string Action) : IRequest<Result<EntitlementCheckResult>>;
