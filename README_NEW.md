# ASB Entitlement Check Service

## Executive Summary

A y entitlement management service implementing **Clean Architecture** and **BIAN (Banking Industry Architecture Network)** standards. Built with .NET 8 and Neo4j graph database, featuring CQRS, comprehensive logging, validation, and error handling.

## Problem Statement

Financial organizations require a centralized, scalable entitlement system to answer: *"Is a given identity entitled to perform a specific action on a resource?"*

This service models identities, roles, permissions, and resources using graph relationships, conforming to BIAN standards for Party Management and Authorization.

## Solution Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────┐
│       API Layer (HTTP)              │
│  Controllers, Middleware, DTOs      │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│    Application Layer (Use Cases)    │
│  CQRS Queries, Validators, Behaviors│
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│     Domain Layer (Business Logic)   │
│  Entities, Value Objects, Interfaces│
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Infrastructure Layer (Technical)   │
│  Neo4j, Repositories, Seeding       │
└─────────────────────────────────────┘
```

### Key Technologies

- **.NET 8**: Modern, high-performance framework
- **Neo4j 5.x**: Graph database for complex relationship queries
- **MediatR**: CQRS implementation with pipeline behaviors
- **FluentValidation**: Declarative validation rules
- **xUnit + Moq**: Unit and integration testing
- **Swagger/OpenAPI**: API documentation

## Graph Data Model (BIAN Compliant)

```cypher
(Identity)-[:HAS_ROLE]->(Role)-[:GRANTS]->(Permission)-[:ON]->(Resource)
```

### BIAN Mapping

| Entity | BIAN Service Domain | Description |
|--------|---------------------|-------------|
| Identity | Party Reference Data Management | Unique customer/user/service identifier |
| Role | Party Role | Groups of permissions (Customer, Admin) |
| Permission | Party Authorization | Actions allowed (ViewAccount, EditAccount) |
| Resource | Product/Service | Objects being accessed (Account, Transaction) |
| Entitlement | Entitlement Management | Access control relationships |

## Features

✅ **Clean Architecture** - Separation of concerns, testability
✅ **CQRS Pattern** - Query/Command segregation with MediatR
✅ **Result Pattern** - Explicit error handling without exceptions
✅ **Validation Pipeline** - Automatic input validation
✅ **Structured Logging** - Performance metrics and tracing
✅ **BIAN Compliance** - Financial services standards
✅ **Type-Safe Configuration** - Options pattern
✅ **Comprehensive Tests** - Unit and integration coverage
✅ **API Documentation** - Swagger/OpenAPI
✅ **Demo Data Seeder** - Quick start with sample data

## API Contract

### Endpoint: `POST /api/entitlements/check`

**Request**:
```json
{
  "identityId": "user1",
  "resourceId": "acc1",
  "action": "ViewAccount"
}
```

**Success Response** (200 OK):
```json
{
  "entitled": true,
  "reason": "Access granted via role-based entitlement",
  "grantedPermission": "ViewAccount",
  "checkedAt": "2026-05-14T22:15:30Z"
}
```

**Denied Response** (200 OK):
```json
{
  "entitled": false,
  "reason": "No matching permission found for the requested action",
  "grantedPermission": null,
  "checkedAt": "2026-05-14T22:15:30Z"
}
```

**Validation Error** (400 Bad Request):
```json
{
  "error": "Validation failed: Identity ID is required; Action must contain only letters"
}
```

## Prerequisites

- **.NET 8 SDK** or later
- **Neo4j 5.x** (local or remote instance)
- **Visual Studio 2022** or **VS Code** (optional)
- **Docker** (optional, for Neo4j container)

## Quick Start

### 1. Install Neo4j

#### Option A: Docker
```bash
docker run -d \
  --name neo4j \
  -p 7474:7474 -p 7687:7687 \
  -e NEO4J_AUTH=neo4j/your-password \
  neo4j:5.13
```

#### Option B: Neo4j Desktop
Download from [neo4j.com/download](https://neo4j.com/download/)

### 2. Configure Connection

Update `appsettings.json`:
```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "your-password",
    "Database": "neo4j"
  }
}
```

### 3. Build and Run

```bash
# Navigate to solution directory
cd Code

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run the API
dotnet run --project src/ASB.Entitlements.csproj
```

The API will start at `https://localhost:5001` (or `http://localhost:5000`)

### 4. Seed Demo Data

The application automatically seeds demo data on startup. To manually trigger:

```bash
# Via API endpoint (if implemented)
curl -X POST https://localhost:5001/api/seed
```

### 5. Test the API

#### Using Swagger UI
Navigate to `https://localhost:5001/swagger`

#### Using curl
```bash
# Alice can view acc1 ✅
curl -X POST https://localhost:5001/api/entitlements/check \
  -H "Content-Type: application/json" \
  -d '{
    "identityId": "user1",
    "resourceId": "acc1",
    "action": "ViewAccount"
  }'

# Alice cannot edit acc1 ❌
curl -X POST https://localhost:5001/api/entitlements/check \
  -H "Content-Type: application/json" \
  -d '{
    "identityId": "user1",
    "resourceId": "acc1",
    "action": "EditAccount"
  }'

# Admin can delete acc1 ✅
curl -X POST https://localhost:5001/api/entitlements/check \
  -H "Content-Type: application/json" \
  -d '{
    "identityId": "admin1",
    "resourceId": "acc1",
    "action": "DeleteAccount"
  }'
```

## Demo Data

The seeder creates the following test scenario:

### Identities
- **user1** (Alice Johnson) - Customer
- **user2** (Bob Smith) - Customer
- **admin1** (Admin User) - Employee

### Roles & Permissions
- **Customer Role** → ViewAccount → acc1, acc2
- **AccountManager Role** → EditAccount + ViewAccount → acc2
- **Admin Role** → ViewAccount + EditAccount + DeleteAccount → acc1, acc2, acc3

### Test Scenarios
| Identity | Resource | Action | Result | Reason |
|----------|----------|--------|--------|--------|
| user1 | acc1 | ViewAccount | ✅ Granted | Has Customer role |
| user1 | acc1 | EditAccount | ❌ Denied | No edit permission |
| user2 | acc2 | EditAccount | ✅ Granted | Has AccountManager role |
| admin1 | acc1 | DeleteAccount | ✅ Granted | Has Admin role |

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test tests/ASB.Entitlements.Tests/

# Run with verbosity
dotnet test --verbosity normal
```

### Test Coverage

- **Unit Tests**: Domain entities, validators, query handlers
- **Integration Tests**: Repository operations, API endpoints
- **Edge Cases**: Missing identities, invalid inputs, database errors

## Project Structure

```
ASB.Entitlements/
├── src/
│   ├── ASB.Entitlements.Domain/           # Core business logic
│   │   ├── Common/                        # Shared abstractions
│   │   ├── Entities/                      # Domain entities
│   │   ├── ValueObjects/                  # Immutable value objects
│   │   └── Repositories/                  # Repository interfaces
│   │
│   ├── ASB.Entitlements.Application/      # Use cases
│   │   ├── Common/Behaviors/              # MediatR pipelines
│   │   ├── Queries/                       # CQRS queries
│   │   └── DependencyInjection.cs
│   │
│   ├── ASB.Entitlements.Infrastructure/   # Technical implementation
│   │   ├── Persistence/                   # Neo4j implementation
│   │   ├── Seeding/                       # Data seeding
│   │   └── DependencyInjection.cs
│   │
│   └── ASB.Entitlements/                  # API layer
│       ├── Controllers/                   # HTTP endpoints
│       ├── Middleware/                    # Exception handling
│       └── Program.cs                     # DI composition root
│
├── tests/
│   ├── ASB.Entitlements.Tests/            # Unit tests
│   └── ASB.Entitlements.IntegrationTests/ # Integration tests
│
├── ARCHITECTURE.md                        # Detailed architecture docs
├── MIGRATION_GUIDE.md                     # Refactoring guide
└── README.md                              # This file
```

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ASB.Entitlements": "Debug"
    }
  },
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

### Environment Variables

```bash
# Override Neo4j connection
export Neo4j__Uri="bolt://production-server:7687"
export Neo4j__Username="app-user"
export Neo4j__Password="secure-password"
```

## Design Patterns

- **Clean Architecture**: Dependency inversion, separation of concerns
- **CQRS**: Command Query Responsibility Segregation
- **Mediator Pattern**: Decoupled request handling
- **Repository Pattern**: Data access abstraction
- **Result Pattern**: Explicit error handling
- **Options Pattern**: Type-safe configuration
- **Pipeline Pattern**: Cross-cutting concerns (logging, validation)

## Performance

- **Async/Await**: Non-blocking I/O throughout
- **Connection Pooling**: Configured Neo4j driver pool
- **Graph Indexes**: Optimized queries with indexed properties
- **Logging**: Performance metrics for all operations

## Security

- **Input Validation**: FluentValidation on all inputs
- **Parameterized Queries**: Prevents Cypher injection
- **Error Handling**: No sensitive data in error messages
- **Logging**: No PII/credentials logged

## Monitoring

- **Structured Logging**: JSON-formatted logs with correlation IDs
- **Performance Metrics**: Request duration tracking
- **Health Checks**: (Recommended: add `/health` endpoint)
- **Distributed Tracing**: (Recommended: add OpenTelemetry)

## Future Enhancements

- [ ] Add Commands for creating/updating entitlements
- [ ] Implement distributed caching (Redis)
- [ ] Add API versioning
- [ ] Implement rate limiting
- [ ] Add health check endpoints
- [ ] Integrate OpenTelemetry for tracing
- [ ] Add Prometheus metrics
- [ ] Implement event sourcing for audit trail
- [ ] Add support for time-bound entitlements
- [ ] Implement hierarchical resources

## Troubleshooting

### Neo4j Connection Issues
```bash
# Check Neo4j is running
docker ps | grep neo4j

# View Neo4j logs
docker logs neo4j

# Test connection
curl http://localhost:7474
```

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Test Failures
```bash
# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Contributing

1. Follow Clean Architecture principles
2. Write unit tests for all new features
3. Use FluentValidation for input validation
4. Follow existing code style and naming conventions
5. Update documentation for architectural changes

## License

Proprietary - ASB Bank

## Contact

For questions or support, contact the development team.

---

**Built with ❤️ using Clean Architecture and BIAN Standards**
