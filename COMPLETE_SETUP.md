# Complete Setup Guide - Principal-Level Architecture

## 🎯 Current Status

✅ **Architecture Created**: 3 new project layers built
✅ **Neo4j Aura Configured**: Your cloud instance ready
✅ **Tests Fixed**: Castle.Core issue resolved
✅ **Documentation Complete**: 6 comprehensive guides

⚠️ **Remaining**: Add new projects to solution and update API project (20 minutes)

## 📋 Complete This Checklist

### ✅ Step 1: Add New Projects to Solution (2 minutes)

```bash
cd C:\Personal\job\ASB\Code

# Add Domain project
dotnet sln add src/ASB.Entitlements.Domain/ASB.Entitlements.Domain.csproj

# Add Application project
dotnet sln add src/ASB.Entitlements.Application/ASB.Entitlements.Application.csproj

# Add Infrastructure project
dotnet sln add src/ASB.Entitlements.Infrastructure/ASB.Entitlements.Infrastructure.csproj

# Verify
dotnet sln list
```

You should see 4 projects listed.

### ✅ Step 2: Update API Project File (3 minutes)

Edit `src/ASB.Entitlements.csproj`:

**Find this section:**
```xml
<ItemGroup>
  <PackageReference Include="Neo4j.Driver" Version="5.13.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
</ItemGroup>
```

**Replace with:**
```xml
<ItemGroup>
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="ASB.Entitlements.Domain\ASB.Entitlements.Domain.csproj" />
  <ProjectReference Include="ASB.Entitlements.Application\ASB.Entitlements.Application.csproj" />
  <ProjectReference Include="ASB.Entitlements.Infrastructure\ASB.Entitlements.Infrastructure.csproj" />
</ItemGroup>
```

**Note**: Remove Neo4j.Driver - it's now in Infrastructure layer.

### ✅ Step 3: Replace Program.cs (2 minutes)

**Backup old file:**
```bash
cd src
copy Program.cs Program.cs.old
```

**Replace `src/Program.cs` with:**
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
        Description = "Graph-backed entitlement service with Clean Architecture and BIAN compliance"
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
        logger.LogError(ex, "Error seeding demo data. Application will continue.");
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

### ✅ Step 4: Replace EntitlementController.cs (3 minutes)

**Backup old file:**
```bash
cd Controllers
copy EntitlementController.cs EntitlementController.cs.old
```

**Replace `src/Controllers/EntitlementController.cs` with:**
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
    /// <param name="request">Entitlement check request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entitlement check result</returns>
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

        _logger.LogDebug("Entitlement check completed: {Entitled}", response.Entitled);

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

### ✅ Step 5: Delete Old Files (1 minute)

```bash
cd C:\Personal\job\ASB\Code\src

# Delete obsolete files
del Data\Neo4jContext.cs
del Data\INeo4jContext.cs
del Data\EntitlementRepository.cs
del Data\DemoDataSeeder.cs
del DomainModels.cs

# If Data folder is empty, delete it
rmdir Data
```

### ✅ Step 6: Build Everything (2 minutes)

```bash
cd C:\Personal\job\ASB\Code

# Clean previous builds
dotnet clean

# Restore packages
dotnet restore

# Build all projects
dotnet build
```

**Expected**: Build succeeded with 0 errors ✅

### ✅ Step 7: Update Tests (5 minutes)

Edit `tests/EntitlementRepositoryTests.cs`:

**Add these using statements at the top:**
```csharp
using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.ValueObjects;
using ASB.Entitlements.Domain.Common;
using ASB.Entitlements.Infrastructure.Persistence;
using ASB.Entitlements.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
```

**Replace both test methods with:**
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
    mockRecord.Setup(r => r["reason"]).Returns("Access granted via role-based entitlement");
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
    mockRecord.Setup(r => r["reason"]).Returns("No matching permission found for the requested action");
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

### ✅ Step 8: Run Tests (2 minutes)

```bash
cd C:\Personal\job\ASB\Code
dotnet test
```

**Expected**: Test Run Successful. Total tests: 2, Passed: 2 ✅

### ✅ Step 9: Run the Application (3 minutes)

```bash
dotnet run --project src/ASB.Entitlements.csproj
```

**Look for these log messages:**
```
Neo4j connection established successfully to neo4j+s://493e8d74.databases.neo4j.io with encryption: Encrypted
Starting demo data seeding...
Creating identities...
Creating roles...
Creating permissions...
Creating resources...
Creating relationships...
Demo data seeding completed successfully
Now listening on: http://localhost:5000
```

### ✅ Step 10: Test the API (2 minutes)

**Open new terminal and run:**

```bash
# Test 1: Alice can view acc1 (GRANTED)
curl -X POST http://localhost:5000/api/entitlement/check -H "Content-Type: application/json" -d "{\"identityId\":\"user1\",\"resourceId\":\"acc1\",\"action\":\"ViewAccount\"}"

# Expected: {"entitled":true,"reason":"Access granted via role-based entitlement","grantedPermission":"ViewAccount",...}
```

```bash
# Test 2: Alice cannot edit acc1 (DENIED)
curl -X POST http://localhost:5000/api/entitlement/check -H "Content-Type: application/json" -d "{\"identityId\":\"user1\",\"resourceId\":\"acc1\",\"action\":\"EditAccount\"}"

# Expected: {"entitled":false,"reason":"No matching permission found for the requested action","grantedPermission":null,...}
```

**Or open Swagger UI**: http://localhost:5000/swagger

### ✅ Step 11: Verify in Neo4j Aura (2 minutes)

1. Open https://console.neo4j.io
2. Click on "Instance01"
3. Click "Query" button
4. Run:

```cypher
// See all demo data
MATCH (n)
RETURN n
LIMIT 100
```

You should see nodes for Identities, Roles, Permissions, and Resources!

### ✅ Step 12: Replace README (1 minute)

```bash
cd C:\Personal\job\ASB\Code
move README.md README_OLD.md
move README_NEW.md README.md
```

## 🎉 Done! You Now Have:

✅ Clean Architecture with 4 layers
✅ CQRS pattern with MediatR
✅ FluentValidation for input validation
✅ Result pattern for error handling
✅ BIAN-compliant domain models
✅ Neo4j Aura cloud database configured
✅ Comprehensive logging
✅ All tests passing
✅ Complete documentation

## 📚 Documentation Available

| File | Purpose |
|------|---------|
| `README.md` | Main project documentation |
| `ARCHITECTURE.md` | Clean Architecture details |
| `MIGRATION_GUIDE.md` | Refactoring explanation |
| `IMPLEMENTATION_SUMMARY.md` | What was built |
| `NEO4J_AURA_SETUP.md` | Your Aura configuration |
| `READY_TO_RUN.md` | Quick test scenarios |
| `COMPLETE_SETUP.md` | This file |

## 🚀 For Submission

Your project demonstrates:
- **Principal-level architecture**
- **Enterprise design patterns**
- **BIAN compliance**
- **Production-ready code**
- **Comprehensive documentation**

**Total time invested**: ~2 hours of refactoring
**Result**: Code that stands out in principal engineer assessment

## ⏰ Time Breakdown

- Reading this guide: 5 min
- Steps 1-6: Build setup: 12 min
- Steps 7-8: Tests: 7 min
- Steps 9-11: Run & verify: 7 min
- Step 12: README: 1 min

**Total: ~30 minutes** to complete the setup!

## 💡 Tips for the Assessment Interview

When discussing with Moshood (Principal Engineer):

1. **Start with architecture**: Explain the 4 layers and why
2. **Highlight patterns**: CQRS, Result, Repository, Options
3. **Show BIAN compliance**: How domain models map to standards
4. **Demonstrate**: Run the app, show Swagger, query Neo4j Aura
5. **Discuss trade-offs**: Why you chose these patterns
6. **Show extensibility**: How easy it is to add new queries
7. **Mention logging**: Performance metrics and observability

Good luck! 🎯
