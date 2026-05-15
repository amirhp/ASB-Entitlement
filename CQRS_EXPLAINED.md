# CQRS Pattern Explained - How It Works in This Project

## What is CQRS?

**CQRS** (Command Query Responsibility Segregation) separates:
- **Commands**: Operations that change state (Create, Update, Delete)
- **Queries**: Operations that read data (no state changes)

## Our Implementation

Currently, we only have **Queries** (entitlement checks don't modify data):

```
API Request → Controller → MediatR → Handler → Repository → Neo4j → Response
```

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│  1. HTTP POST /api/entitlement/check                            │
│     { "identityId": "user1", "resourceId": "acc1",              │
│       "action": "ViewAccount" }                                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. EntitlementController                                       │
│     - Receives HTTP request                                     │
│     - Creates CheckEntitlementQuery                             │
│     - Sends to MediatR                                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. MediatR Pipeline                                            │
│                                                                 │
│     ┌─────────────────────────────────────────┐                │
│     │  ValidationBehavior                     │                │
│     │  - Validates query parameters           │                │
│     │  - Returns error if invalid             │                │
│     └──────────────────┬──────────────────────┘                │
│                        │                                        │
│     ┌──────────────────▼──────────────────────┐                │
│     │  LoggingBehavior                        │                │
│     │  - Logs request start                   │                │
│     │  - Measures performance                 │                │
│     │  - Logs request completion              │                │
│     └──────────────────┬──────────────────────┘                │
│                        │                                        │
└────────────────────────┼────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│  4. CheckEntitlementQueryHandler                                │
│     - Receives validated query                                  │
│     - Calls repository                                          │
│     - Wraps result in Result<T>                                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  5. IEntitlementRepository (Interface)                          │
│     - Domain contract                                           │
│     - Infrastructure implements it                              │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  6. EntitlementRepository (Infrastructure)                      │
│     - Executes Cypher query                                     │
│     - Handles Neo4j errors                                      │
│     - Returns Result<EntitlementCheckResult>                    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  7. Neo4j Aura Database                                         │
│     MATCH (i:Identity)-[:HAS_ROLE]->(r:Role)                    │
│           -[:GRANTS]->(p:Permission)-[:ON]->(res:Resource)      │
│     WHERE i.id = 'user1' AND res.id = 'acc1'                    │
│           AND p.action = 'ViewAccount'                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  8. Response Journey Back                                       │
│     Repository → Handler → MediatR → Controller → HTTP Response │
└─────────────────────────────────────────────────────────────────┘
```

## Code Flow Example

### 1. Controller Receives Request

```csharp
// EntitlementController.cs
[HttpPost("check")]
public async Task<IActionResult> Check(
    [FromBody] EntitlementCheckRequest request,
    CancellationToken cancellationToken)
{
    // Step 1: Create Query object
    var query = new CheckEntitlementQuery(
        request.IdentityId,
        request.ResourceId,
        request.Action);

    // Step 2: Send to MediatR (magic happens here!)
    var result = await _mediator.Send(query, cancellationToken);

    // Step 3: Convert Result to HTTP response
    if (result.IsFailure)
        return BadRequest(new ErrorResponse { Error = result.Error });

    return Ok(new EntitlementCheckResponse
    {
        Entitled = result.Value.IsEntitled,
        Reason = result.Value.Reason,
        GrantedPermission = result.Value.GrantedPermission
    });
}
```

### 2. MediatR Routes to Handler

MediatR automatically:
1. Finds the handler for `CheckEntitlementQuery`
2. Runs it through pipeline behaviors (validation, logging)
3. Executes the handler
4. Returns the result

```csharp
// CheckEntitlementQuery.cs
public sealed record CheckEntitlementQuery(
    string IdentityId,
    string ResourceId,
    string Action) : IRequest<Result<EntitlementCheckResult>>;
//                   ^^^^^^^^^ MediatR uses this to find handler
```

### 3. Validation Runs First

```csharp
// CheckEntitlementQueryValidator.cs
public class CheckEntitlementQueryValidator : AbstractValidator<CheckEntitlementQuery>
{
    public CheckEntitlementQueryValidator()
    {
        RuleFor(x => x.IdentityId)
            .NotEmpty().WithMessage("Identity ID is required");

        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource ID is required");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required")
            .Matches("^[a-zA-Z]+$").WithMessage("Action must contain only letters");
    }
}
```

If validation fails, the pipeline stops and returns an error. Handler never executes!

### 4. Logging Wraps Execution

```csharp
// LoggingBehavior.cs
public async Task<TResponse> Handle(...)
{
    var stopwatch = Stopwatch.StartNew();
    _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);

    var response = await next(); // Execute the actual handler

    stopwatch.Stop();
    _logger.LogInformation("Handled in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

    return response;
}
```

### 5. Handler Executes Business Logic

```csharp
// CheckEntitlementQueryHandler.cs
public class CheckEntitlementQueryHandler
    : IRequestHandler<CheckEntitlementQuery, Result<EntitlementCheckResult>>
{
    private readonly IEntitlementRepository _repository;

    public async Task<Result<EntitlementCheckResult>> Handle(
        CheckEntitlementQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking entitlement for {IdentityId}", request.IdentityId);

        var result = await _repository.CheckEntitlementAsync(
            request.IdentityId,
            request.ResourceId,
            request.Action,
            cancellationToken);

        return result; // Result<EntitlementCheckResult>
    }
}
```

### 6. Repository Queries Database

```csharp
// EntitlementRepository.cs (Infrastructure)
public async Task<Result<EntitlementCheckResult>> CheckEntitlementAsync(...)
{
    const string query = @"
        MATCH (i:Identity {id: $identityId})-[:HAS_ROLE]->
              (r:Role)-[:GRANTS]->(p:Permission)-[:ON]->
              (res:Resource {id: $resourceId})
        WHERE p.action = $action
        RETURN ...";

    var session = _context.GetSession();
    try
    {
        var cursor = await session.RunAsync(query, new { identityId, resourceId, action });
        // ... process results
        return Result.Success(EntitlementCheckResult.Granted(...));
    }
    catch (Exception ex)
    {
        return Result.Failure<EntitlementCheckResult>(ex.Message);
    }
}
```

## Why CQRS with MediatR?

### ✅ Benefits

1. **Separation of Concerns**
   - Controller doesn't know about repository
   - Handler doesn't know about HTTP
   - Each layer has one responsibility

2. **Easy to Test**
   - Test validator independently
   - Test handler with mocked repository
   - Test controller with mocked MediatR

3. **Cross-Cutting Concerns**
   - Validation happens automatically
   - Logging happens automatically
   - Performance tracking happens automatically

4. **Extensibility**
   - Add new query? Just create Query, Validator, Handler
   - Add new behavior? Register it once, applies to all queries
   - No changes to existing code!

5. **No Coupling**
   - Controller → MediatR (interface)
   - Handler → Repository (interface)
   - Each can change independently

### Example: Adding a New Query

Want to add "Get all roles for an identity"?

```csharp
// 1. Create Query
public record GetIdentityRolesQuery(string IdentityId)
    : IRequest<Result<IReadOnlyList<string>>>;

// 2. Create Validator
public class GetIdentityRolesQueryValidator : AbstractValidator<GetIdentityRolesQuery>
{
    public GetIdentityRolesQueryValidator()
    {
        RuleFor(x => x.IdentityId).NotEmpty();
    }
}

// 3. Create Handler
public class GetIdentityRolesQueryHandler
    : IRequestHandler<GetIdentityRolesQuery, Result<IReadOnlyList<string>>>
{
    private readonly IEntitlementRepository _repository;

    public async Task<Result<IReadOnlyList<string>>> Handle(...)
    {
        return await _repository.GetIdentityRolesAsync(request.IdentityId);
    }
}

// 4. Add Controller endpoint
[HttpGet("{identityId}/roles")]
public async Task<IActionResult> GetRoles(string identityId)
{
    var result = await _mediator.Send(new GetIdentityRolesQuery(identityId));
    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
}
```

**That's it!** Validation and logging automatically apply. No changes to existing code!

## What About Commands?

For future expansion (not needed for entitlement checks):

```csharp
// Create a new role
public record CreateRoleCommand(string Id, string Name, string Description)
    : IRequest<Result<Role>>;

// Handler would create the role
public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Role>>
{
    // ... implementation
}
```

Commands follow the same pattern but modify state.

## Pipeline Order

```
Request
   ↓
ValidationBehavior (validates input)
   ↓
LoggingBehavior (logs + measures performance)
   ↓
Handler (executes business logic)
   ↓
Response
```

All behaviors execute for **every request** automatically!

## Summary

**CQRS in this project means:**
- ✅ Queries are separate objects (CheckEntitlementQuery)
- ✅ Each query has a dedicated handler
- ✅ MediatR routes requests to handlers
- ✅ Pipeline behaviors add cross-cutting concerns
- ✅ Controller is thin - just translates HTTP ↔ MediatR
- ✅ Easy to test each component in isolation
- ✅ Easy to add new queries without changing existing code

**This is a professional, scalable architecture!** 🚀
