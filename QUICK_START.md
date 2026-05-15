# Quick Start - Complete the Migration in 30 Minutes

Follow these steps exactly to complete the principal-level refactoring.

## ✅ Step 1: Add New Projects to Solution (2 minutes)

```bash
cd Code

# Add Domain project
dotnet sln add src/ASB.Entitlements.Domain/ASB.Entitlements.Domain.csproj

# Add Application project
dotnet sln add src/ASB.Entitlements.Application/ASB.Entitlements.Application.csproj

# Add Infrastructure project
dotnet sln add src/ASB.Entitlements.Infrastructure/ASB.Entitlements.Infrastructure.csproj
```

## ✅ Step 2: Update API Project References (1 minute)

Edit `src/ASB.Entitlements.csproj`, add to `<ItemGroup>`:

```xml
<ProjectReference Include="ASB.Entitlements.Domain\ASB.Entitlements.Domain.csproj" />
<ProjectReference Include="ASB.Entitlements.Application\ASB.Entitlements.Application.csproj" />
<ProjectReference Include="ASB.Entitlements.Infrastructure\ASB.Entitlements.Infrastructure.csproj" />
```

Remove the direct Neo4j.Driver package reference (it's in Infrastructure now).

## ✅ Step 3: Build to Verify Structure (1 minute)

```bash
dotnet restore
dotnet build
```

All 4 projects should build successfully.

## ✅ Step 4: Update appsettings.json (1 minute)

Replace content with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ASB.Entitlements": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "password",
    "Database": "neo4j",
    "MaxConnectionPoolSize": 50,
    "ConnectionTimeout": "00:00:30"
  }
}
```

## ✅ Step 5: Replace Program.cs (2 minutes)

Create new `src/Program.cs`:

```csharp
using ASB.Entitlements.Application;
using ASB.Entitlements.Infrastructure;
using ASB.Entitlements.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "ASB Entitlements API",
        Version = "v1",
        Description = "Graph-backed entitlement service with BIAN compliance"
    });
});

// Add application layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Seed demo data on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Starting demo data seeding...");
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Demo data seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding demo data");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## ✅ Step 6: Replace EntitlementController.cs (3 minutes)

Replace `src/Controllers/EntitlementController.cs`:

```csharp
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
```

## ✅ Step 7: Delete Old Files (1 minute)

```bash
cd src

# Delete obsolete files
rm Data/Neo4jContext.cs
rm Data/INeo4jContext.cs
rm Data/EntitlementRepository.cs
rm Data/DemoDataSeeder.cs
rm DomainModels.cs

# Optional: delete empty Data folder if nothing else is in it
rmdir Data
```

## ✅ Step 8: Update Tests (3 minutes)

Edit `tests/EntitlementRepositoryTests.cs`, add using statements at top:

```csharp
using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.ValueObjects;
using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Infrastructure.Persistence;
using ASB.Entitlements.Infrastructure.Persistence.Repositories;
```

Update tests to use new Result pattern:

```csharp
[Fact]
public async Task CheckEntitlementAsync_ReturnsTrue_WhenEntitled()
{
    // Arrange
    var mockContext = new Mock<INeo4jContext>();
    var mockSession = new Mock<IAsyncSession>();
    var mockCursor = new Mock<IResultCursor>();
    var mockRecord = new Mock<IRecord>();

    mockRecord.Setup(r => r["entitled"]).Returns(true);
    mockRecord.Setup(r => r["reason"]).Returns("Access granted");
    mockRecord.Setup(r => r["permissionName"]).Returns("ViewAccount");

    mockCursor.Setup(c => c.Current).Returns(mockRecord.Object);
    mockCursor.SetupSequence(c => c.FetchAsync())
        .ReturnsAsync(true)
        .ReturnsAsync(false);

    mockSession.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<object>()))
        .ReturnsAsync(mockCursor.Object);
    mockSession.Setup(s => s.CloseAsync()).Returns(Task.CompletedTask);

    mockContext.Setup(c => c.GetSession()).Returns(mockSession.Object);

    var logger = new Mock<ILogger<EntitlementRepository>>();
    var repo = new EntitlementRepository(mockContext.Object, logger.Object);

    // Act
    var result = await repo.CheckEntitlementAsync("user1", "acc1", "ViewAccount");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(result.Value.IsEntitled);
    Assert.Equal("ViewAccount", result.Value.GrantedPermission);
}

[Fact]
public async Task CheckEntitlementAsync_ReturnsFalse_WhenNotEntitled()
{
    // Arrange
    var mockContext = new Mock<INeo4jContext>();
    var mockSession = new Mock<IAsyncSession>();
    var mockCursor = new Mock<IResultCursor>();
    var mockRecord = new Mock<IRecord>();

    mockRecord.Setup(r => r["entitled"]).Returns(false);
    mockRecord.Setup(r => r["reason"]).Returns("No permission found");
    mockRecord.Setup(r => r["permissionName"]).Returns((string?)null);

    mockCursor.Setup(c => c.Current).Returns(mockRecord.Object);
    mockCursor.SetupSequence(c => c.FetchAsync())
        .ReturnsAsync(true)
        .ReturnsAsync(false);

    mockSession.Setup(s => s.RunAsync(It.IsAny<string>(), It.IsAny<object>()))
        .ReturnsAsync(mockCursor.Object);
    mockSession.Setup(s => s.CloseAsync()).Returns(Task.CompletedTask);

    mockContext.Setup(c => c.GetSession()).Returns(mockSession.Object);

    var logger = new Mock<ILogger<EntitlementRepository>>();
    var repo = new EntitlementRepository(mockContext.Object, logger.Object);

    // Act
    var result = await repo.CheckEntitlementAsync("user1", "acc2", "EditAccount");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.False(result.Value.IsEntitled);
}
```

## ✅ Step 9: Build and Test (2 minutes)

```bash
# Clean build
dotnet clean
dotnet build

# Run tests
dotnet test
```

All tests should pass! ✅

## ✅ Step 10: Start Neo4j (if not running) (2 minutes)

```bash
docker run -d \
  --name neo4j \
  -p 7474:7474 -p 7687:7687 \
  -e NEO4J_AUTH=neo4j/password \
  neo4j:5.13
```

Or start Neo4j Desktop.

## ✅ Step 11: Run and Test API (3 minutes)

```bash
dotnet run --project src/ASB.Entitlements.csproj
```

### Test with curl:

```bash
# Alice can view acc1 ✅
curl -X POST http://localhost:5000/api/entitlement/check \
  -H "Content-Type: application/json" \
  -d '{"identityId":"user1","resourceId":"acc1","action":"ViewAccount"}'

# Alice cannot edit acc1 ❌
curl -X POST http://localhost:5000/api/entitlement/check \
  -H "Content-Type: application/json" \
  -d '{"identityId":"user1","resourceId":"acc1","action":"EditAccount"}'

# Admin can delete ✅
curl -X POST http://localhost:5000/api/entitlement/check \
  -H "Content-Type: application/json" \
  -d '{"identityId":"admin1","resourceId":"acc1","action":"DeleteAccount"}'
```

Or open Swagger UI: `http://localhost:5000/swagger`

## ✅ Step 12: Replace README (1 minute)

```bash
cd Code
mv README.md README_OLD.md
mv README_NEW.md README.md
```

## ✅ Done! 🎉

You now have a **principal-level Clean Architecture implementation** with:

✅ 4 properly layered projects
✅ CQRS with MediatR
✅ FluentValidation
✅ Result pattern
✅ BIAN-compliant domain models
✅ Comprehensive logging
✅ Type-safe configuration
✅ All tests passing
✅ Complete documentation

## 📚 Review Documentation

- `README.md` - Main project documentation
- `ARCHITECTURE.md` - Detailed architecture explanation
- `MIGRATION_GUIDE.md` - How the refactoring was done
- `IMPLEMENTATION_SUMMARY.md` - What was built and why

## 🚀 Next Steps

1. Review the code structure
2. Read ARCHITECTURE.md to understand design decisions
3. Run through all test scenarios
4. Prepare to discuss architecture choices with Moshood
5. Submit with confidence!

**Time Total**: ~20-30 minutes
**Result**: Principal-level code ready for review!
