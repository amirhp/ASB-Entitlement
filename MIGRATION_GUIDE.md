# Migration Guide - Refactoring to Principal-Level Architecture

## What's New

This refactoring transforms the codebase from a basic 3-tier architecture to a **Clean Architecture** with the following improvements:

### Architecture Changes
1. **New Layered Structure**:
   - Domain Layer (business logic)
   - Application Layer (use cases with CQRS)
   - Infrastructure Layer (technical concerns)
   - API Layer (HTTP interface)

2. **Design Patterns Added**:
   - CQRS with MediatR
   - Repository Pattern with proper interfaces
   - Result Pattern for error handling
   - Options Pattern for configuration
   - Pipeline Behaviors for cross-cutting concerns

3. **BIAN Compliance**:
   - Proper domain models aligned with BIAN standards
   - Enhanced entity models with business logic
   - Value objects for domain concepts

## Project Structure

### Old Structure
```
src/
  ├── Controllers/
  ├── Data/
  ├── Models/
  ├── Program.cs
  └── appsettings.json
```

### New Structure
```
src/
  ├── ASB.Entitlements.Domain/          # Domain layer
  │   ├── Common/
  │   ├── Entities/
  │   ├── ValueObjects/
  │   └── Repositories/
  │
  ├── ASB.Entitlements.Application/     # Application layer
  │   ├── Common/Behaviors/
  │   ├── Queries/
  │   └── DependencyInjection.cs
  │
  ├── ASB.Entitlements.Infrastructure/  # Infrastructure layer
  │   ├── Persistence/
  │   ├── Seeding/
  │   └── DependencyInjection.cs
  │
  └── ASB.Entitlements (API)/            # API layer (update existing project)
      ├── Controllers/
      ├── Middleware/
      ├── Program.cs
      └── appsettings.json
```

## Step-by-Step Migration

### Step 1: Add New Project References to API Project

Update `src/ASB.Entitlements.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="ASB.Entitlements.Application\ASB.Entitlements.Application.csproj" />
  <ProjectReference Include="ASB.Entitlements.Infrastructure\ASB.Entitlements.Infrastructure.csproj" />
</ItemGroup>
```

### Step 2: Update Program.cs

Replace the existing `Program.cs` with the new layered architecture setup.

### Step 3: Update appsettings.json

Add Neo4j configuration section:

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "your-password",
    "Database": "neo4j",
    "MaxConnectionPoolSize": 50,
    "ConnectionTimeout": "00:00:30"
  }
}
```

### Step 4: Update Controllers

Refactor controllers to use MediatR instead of direct repository access.

### Step 5: Delete Old Files

After migration, delete these obsolete files:
- `src/Data/Neo4jContext.cs` (moved to Infrastructure)
- `src/Data/EntitlementRepository.cs` (moved to Infrastructure)
- `src/Data/DemoDataSeeder.cs` (moved to Infrastructure)
- `src/DomainModels.cs` (replaced by Domain entities)
- `src/Data/INeo4jContext.cs` (moved to Infrastructure)

## Key API Changes

### Old Controller Pattern
```csharp
public class EntitlementController : ControllerBase
{
    private readonly EntitlementRepository _repository;

    [HttpPost]
    public async Task<IActionResult> Check(EntitlementCheckRequest request)
    {
        var result = await _repository.CheckEntitlementAsync(...);
        // ...
    }
}
```

### New Controller Pattern
```csharp
public class EntitlementController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Check(
        [FromBody] CheckEntitlementRequest request,
        CancellationToken cancellationToken)
    {
        var query = new CheckEntitlementQuery(
            request.IdentityId,
            request.ResourceId,
            request.Action);

        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(new CheckEntitlementResponse(result.Value))
            : BadRequest(new { Error = result.Error });
    }
}
```

## Testing Changes

### Old Test Pattern
```csharp
var mockContext = new Mock<INeo4jContext>();
var repo = new EntitlementRepository(mockContext.Object);
```

### New Test Pattern
```csharp
// Unit test the query handler
var mockRepo = new Mock<IEntitlementRepository>();
var handler = new CheckEntitlementQueryHandler(mockRepo.Object, logger);

// Unit test the validator
var validator = new CheckEntitlementQueryValidator();
var result = await validator.ValidateAsync(query);
```

## Benefits of New Architecture

1. **Testability**: Each layer can be tested independently
2. **Maintainability**: Clear separation of concerns
3. **Extensibility**: Easy to add new features without modifying existing code
4. **Performance**: Pipeline behaviors for logging and monitoring
5. **Validation**: Automatic validation before processing
6. **Error Handling**: Consistent error handling with Result pattern
7. **BIAN Compliance**: Domain models aligned with banking standards
8. **Logging**: Structured logging throughout all layers
9. **Configuration**: Type-safe configuration with Options pattern
10. **Documentation**: Better code organization and self-documenting structure

## Breaking Changes

None - The API contract remains the same. Only internal implementation changes.

## Backwards Compatibility

The REST API endpoints remain unchanged:
- `POST /api/entitlements/check`

Request and response formats are compatible with the old version.

## Next Steps After Migration

1. Run existing tests to ensure functionality
2. Add new unit tests for validators and handlers
3. Add integration tests for full flow
4. Update documentation
5. Configure monitoring and logging
6. Set up health checks
7. Add API versioning
8. Implement caching if needed

## Rollback Plan

If issues occur:
1. Keep old `src/` folder backed up
2. Switch back to old project references
3. Restore old `Program.cs`
4. Delete new layer folders

## Support

For questions or issues during migration:
1. Review ARCHITECTURE.md for design decisions
2. Check existing tests for usage examples
3. Review MediatR and FluentValidation documentation
