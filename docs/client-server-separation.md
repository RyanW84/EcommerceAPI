# Client-Server Separation Checklist

_Date: 2025-09-28_

The goal of the client-server constraint is to keep the user interface and application state independent so they can evolve at different cadences. The following notes capture how the E-Commerce API solution enforces this boundary.

## Current Guarantees

- **Independent Projects**: The solution contains discrete projects for the Web API (`ECommerceApp.RyanW84`) and the Spectre.Console client (`ECommerceApp.ConsoleClient`). The API project excludes the `ConsoleClient` source from compilation ensuring no accidental linkage.
- **HTTP-only Communication**: `ECommerceApp.ConsoleClient` interacts with the backend exclusively through `HttpClient` calls with request/response DTOs defined under `ConsoleClient/Models`. No server-side repositories, EF Core contexts, or data models are referenced in the console project.
- **Server-side Focus**: The Web API only references infrastructure packages (ASP.NET Core, EF Core, FluentValidation). UI libraries such as Spectre.Console are absent, and dependency injection registers only domain services.
- **Architecture Tests**: `tests/ArchitectureTests/ClientServerSeparationTests.cs` leverages NetArchTest to verify:
  - The API assembly does not depend on `ECommerceApp.ConsoleClient`.
  - The console client does not depend on server-only namespaces (`Data`, `Services`, `Repositories`, `Controllers`) or EF Core.
  - Controllers remain UI-agnostic by avoiding Spectre.Console.

## Operational Guidance

- **Extending the API**: Add new services, repositories, or controllers under the server project. Share schemas with clients via HTTP response contracts, not shared in-process models.
- **Extending the Console Client**: Prefer composing new view models locally or via dedicated DTO responses. Avoid adding project references to `ECommerceApp.RyanW84`—architecture tests will fail if this happens.
- **Automated Enforcement**: Run `dotnet test` to execute the architecture suite whenever dependencies change. Any breach of the client-server boundary results in a failing test to surface the regression early.

## Next Steps

- Expand architecture rules as new UI surfaces or integration points are introduced (for example, a web front end or mobile client).
- Consider publishing a shared SDK package (pure DTOs + clients) if multiple consumers need access to typed models while keeping the server isolated.
