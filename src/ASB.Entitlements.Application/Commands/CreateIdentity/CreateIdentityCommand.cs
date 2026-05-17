using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Domain.Entities;
using MediatR;

namespace ASB.Entitlements.Application.Commands.CreateIdentity;

public sealed record CreateIdentityCommand(
    string Id,
    string Name,
    IdentityType Type
) : IRequest<Result<Identity>>;
