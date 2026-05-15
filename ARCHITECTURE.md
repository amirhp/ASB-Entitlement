# ASB Entitlements Service - Architecture Documentation

## Overview

This service implements a graph-backed entitlement system using **Clean Architecture** principles, conforming to **BIAN (Banking Industry Architecture Network)** standards for financial services.

## Architecture Layers

### 1. Domain Layer (`ASB.Entitlements.Domain`)

**Purpose**: Contains the core business logic and domain models.

**Key Components**:
- **Entities**: Core domain objects with business logic
  - `Identity`: Represents a party (customer, user, service) - BIAN Party Reference
  - `Role`: Party roles as per BIAN standards
  - `Permission`: Authorization entitlements
  - `Resource`: Protected assets/objects

- **Value Objects**: Immutable objects defined by their attributes
  - `EntitlementCheckResult`: Result of an entitlement evaluation

- **Common**: Shared domain concepts
  - `Entity`: Base class for all entities
  - `Result<T>`: Result pattern for error handling without exceptions

- **Repositories**: Interfaces defining data access contracts
  - `IEntitlementRepository`: Entitlement check and query operations

**Design Principles**:
- No dependencies on external layers
- Rich domain models with encapsulated business logic
- Immutability where appropriate
- Domain-driven design principles

### 2. Application Layer (`ASB.Entitlements.Application`)

**Purpose**: Orchestrates application logic, implements use cases.

**Key Components**:
- **Queries** (CQRS Pattern):
  - `CheckEntitlementQuery`: Query to check entitlements
  - Query handlers implementing business workflows

- **Behaviors** (MediatR Pipeline):
  - `ValidationBehavior<,>`: Automatic request validation
  - `LoggingBehavior<,>`: Performance logging and monitoring

- **Validators** (FluentValidation):
  - Input validation rules
  - Business rule validation

**Technologies**:
- **MediatR**: Command/Query handling with pipeline
- **FluentValidation**: Declarative validation rules
- **Microsoft.Extensions.Logging**: Structured logging

**Design Patterns**:
- CQRS (Command Query Responsibility Segregation)
- Mediator pattern
- Pipeline pattern for cross-cutting concerns
- Result pattern for error handling

### 3. Infrastructure Layer (`ASB.Entitlements.Infrastructure`)

**Purpose**: Implements technical concerns and external integrations.

**Key Components**:
- **Persistence**:
  - `Neo4jContext`: Database connection management
  - `EntitlementRepository`: Graph database operations
  - `Neo4jSettings`: Configuration management

- **Seeding**:
  - `IDataSeeder`: Data seeding contract
  - `DemoDataSeeder`: Demo data population

**Technologies**:
- **Neo4j.Driver**: Graph database connectivity
- **Microsoft.Extensions.Options**: Configuration binding

**Features**:
- Connection pooling and management
- Structured logging for all operations
- Configuration-based setup

### 4. API Layer (`ASB.Entitlements`)

**Purpose**: HTTP API interface, request/response handling.

**Key Components**:
- **Controllers**:
  - `EntitlementController`: REST endpoints for entitlement checks

- **Middleware**:
  - Global exception handling
  - Request/response logging

- **Configuration**:
  - Dependency injection setup
  - OpenAPI/Swagger documentation

## BIAN Mapping

This service implements the following BIAN service domains:

| BIAN Service Domain | Implementation |
|---------------------|----------------|
| Party Reference Data Management | Identity entity |
| Party Role | Role entity |
| Party Authorization | Permission entity |
| Product/Service | Resource entity |
| Entitlement Management | EntitlementRepository |

## Graph Data Model

```
(Identity)-[:HAS_ROLE]->(Role)-[:GRANTS]->(Permission)-[:ON]->(Resource)
```

**Node Properties**:
- All nodes have: `id`, `name`, `isActive`, `createdAt`
- **Permission**: `action`, `description`, `scope`
- **Resource**: `type`, `description`, `classification`
- **Identity**: `type` (Customer/Employee/Service/System)
- **Role**: `description`, `type` (SystemDefined/Custom/Temporary)

## Request Flow

```
HTTP Request
    ↓
Controller → MediatR
    ↓
Validation Behavior
    ↓
Logging Behavior
    ↓
Query Handler
    ↓
Repository (Domain Interface)
    ↓
Infrastructure Implementation
    ↓
Neo4j Graph Database
```

## Error Handling Strategy

1. **Domain Layer**: Uses `Result<T>` pattern
2. **Application Layer**: Validation failures return structured errors
3. **Infrastructure Layer**: Catches database exceptions, logs, and returns Results
4. **API Layer**: Translates Results to appropriate HTTP status codes

## Key Design Decisions

### 1. Clean Architecture
- **Why**: Separation of concerns, testability, maintainability
- **Benefit**: Business logic independent of frameworks and databases

### 2. CQRS with MediatR
- **Why**: Separation of reads/writes, extensibility
- **Benefit**: Easy to add new use cases without modifying existing code

### 3. Result Pattern
- **Why**: Avoid exceptions for business logic failures
- **Benefit**: Explicit error handling, better performance

### 4. FluentValidation
- **Why**: Declarative, testable validation rules
- **Benefit**: Validation logic separated from business logic

### 5. Repository Pattern
- **Why**: Abstract data access
- **Benefit**: Easy to test, swap implementations

### 6. Options Pattern
- **Why**: Type-safe configuration
- **Benefit**: Compile-time checking, dependency injection support

## Testing Strategy

### Unit Tests
- Domain entity behavior
- Application query handlers (with mocked repositories)
- Validators
- Repository implementations (with mocked Neo4j context)

### Integration Tests
- API endpoints
- Database operations
- Full request/response cycle

## Performance Considerations

1. **Connection Pooling**: Neo4j driver manages connection pool
2. **Async Operations**: All I/O operations are asynchronous
3. **Indexed Queries**: Graph queries optimized with node property indexes
4. **Logging**: Structured logging with performance metrics

## Security Considerations

1. **Input Validation**: All inputs validated before processing
2. **Parameterized Queries**: Prevent Cypher injection
3. **Error Messages**: No sensitive information in error responses
4. **Logging**: PII/sensitive data not logged

## Scalability

- **Stateless API**: Horizontal scaling supported
- **Connection Pooling**: Efficient database connection usage
- **Graph Database**: Neo4j handles complex relationship queries efficiently
- **Async Processing**: Non-blocking I/O operations

## Monitoring and Observability

- Structured logging with correlation IDs
- Performance metrics for all operations
- Request/response logging
- Database query logging

## Future Enhancements

1. **Caching**: Add distributed caching for frequently accessed entitlements
2. **Events**: Publish domain events for audit trails
3. **Commands**: Add CQRS commands for managing entitlements
4. **API Versioning**: Support multiple API versions
5. **Rate Limiting**: Protect API from abuse
6. **Health Checks**: Monitor system health
7. **Metrics**: Prometheus/Grafana integration
