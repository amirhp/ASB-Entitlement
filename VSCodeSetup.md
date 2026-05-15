# VS Code .NET & Neo4j Setup

## Prerequisites
- Install [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Install [Neo4j Desktop](https://neo4j.com/download/) or run a Neo4j instance (default: bolt://localhost:7687)
- Recommended: Install the C# Dev Kit and .NET Install Tool extensions in VS Code

## Getting Started

1. **Restore dependencies**
   ```sh
   dotnet restore
   ```
2. **Build the solution**
   ```sh
   dotnet build
   ```
3. **Run the API**
   ```sh
   dotnet run --project src
   ```
   The API will be available at http://localhost:5000 or https://localhost:5001 (see launchSettings.json).

4. **Swagger UI**
   Open http://localhost:5000/swagger to explore and test the API.

5. **Run Unit Tests**
   ```sh
   dotnet test tests
   ```

## VS Code Integration
- Open the workspace in VS Code.
- Use the built-in Run & Debug panel to launch and debug the API.
- Use the Test Explorer to run and view test results.

## Neo4j Configuration
- Update `src/appsettings.json` if your Neo4j instance uses a different URI, username, or password.

---

For any issues, check the README.md or reach out for support.
