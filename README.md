# ASB Entitlement Check Service

## Problem Statement
A financial organization requires a centralized entitlement system to answer the question: "Is a given identity entitled to perform a specific action on a resource?" The system must model identities, roles, permissions, and resources according to BIAN standards, using a graph database (Neo4j).

## Solution Overview
- **Graph Model:**
  - Identity → Role → Permission → Resource
  - Entitlements are modeled as relationships in the graph.
- **REST API:**
  - Exposes an endpoint to check entitlements.
  - Accepts: subject (identity), permission name (action), resource identifier.
  - Returns: allow/deny, reason, and which permission was granted.
- **Demo Data:**
  - Seed script or endpoint to load demo identities, roles, permissions, and resources.
- **Testing:**
  - Unit tests for positive and negative entitlement checks.

## Entities
| Entity     | Description                                  |
|------------|----------------------------------------------|
| Identity   | Unique user/customer/service identifier      |
| Role       | Group of permissions (e.g., Customer, Admin) |
| Permission | Action allowed (e.g., ViewAccount)           |
| Resource   | Object being accessed (e.g., Account)        |
| Entitlement| Relationship granting access                 |

## API Contract
- **Endpoint:** `POST /api/entitlements/check`
- **Request:**
  ```json
  {
    "identityId": "string",
    "resourceId": "string",
    "action": "string"
  }
  ```
- **Response:**
  ```json
  {
    "entitled": true/false,
    "reason": "string",
    "grantedPermission": "string (if any)"
  }
  ```

## Demo Data Seeding
- Loads sample identities, roles, permissions, resources, and entitlements.
- Demonstrates both allowed and denied scenarios.

## Testing
- Unit tests for:
  - Entitlement granted
  - Entitlement denied
  - Edge cases (missing identity, resource, etc.)

## Technical Stack
- .NET 8.0/10 (ASP.NET Core Web API)
- Neo4j (graph database)
- No pre-built authorization libraries

## Setup & Usage

### Prerequisites
- .NET 8.0 or 10 SDK
- Neo4j instance (local or remote)

### Configuration
Set the Neo4j connection string, username, and password in your appsettings or environment variables as needed.

### Running the Service
1. Restore dependencies:
  ```sh
  dotnet restore
  ```
2. Build and run the API:
  ```sh
  dotnet run --project src
  ```

### Seeding Demo Data
The service includes a seeder utility. Call the seeding method at startup or expose an endpoint to trigger it. (See `DemoDataSeeder` in `src/Data`.)

### Running Unit Tests
1. Navigate to the test project:
  ```sh
  cd tests
  ```
2. Run tests:
  ```sh
  dotnet test
  ```

---

_For questions or improvements, update this README as the project evolves._

## Planning & Next Steps
1. Define domain models (C# classes for Identity, Role, Permission, Resource, Entitlement).
2. Set up Neo4j integration (repository/service layer).
3. Implement REST API controller for entitlement checks.
4. Add demo data seeding (script or endpoint).
5. Write unit tests for entitlement logic and API.
6. Document setup and usage in this README.

## Open Questions & Assumptions
- BIAN mapping: Using generic resource/action names unless specified.
- RBAC model assumed (roles grant permissions to identities).
- No authentication/authorization for API itself (focus on entitlement logic).
- Error handling: Standardized error messages.

---

_This document will be updated as implementation progresses._
