using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using MediatR;

namespace ASB.Entitlements.Application.Queries.GetIdentity;

public sealed record GetIdentityByIdQuery(string Id) : IRequest<Result<Identity>>;
