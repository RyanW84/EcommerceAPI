# Stateless Request Posture

_Date: 2025-09-28_

## Observations

- The ASP.NET Core host in `Program.cs` does **not** register `AddSession`, `AddDistributedMemoryCache`, or any per-user server session features. The middleware pipeline consists of routing, HTTPS redirection, authorization, and global exception handling.
- Controller and service constructors only request repository/service abstractions from dependency injection. No code stores `HttpContext` instances, `ISession`, or mutable singletons that would outlive a request.
- Domain state is persisted in the database through Entity Framework. All API operations require the caller to submit the necessary context (identifiers, payload DTOs, query parameters) on each call.

## Automated Verification

`tests/ArchitectureTests/ClientServerSeparationTests.ApiAssemblyShouldRemainStateless` ensures the Web API assembly never references session-specific packages:

- `Microsoft.AspNetCore.Session`
- `Microsoft.AspNetCore.Http.ISession`
- `Microsoft.AspNetCore.Http.IHttpContextAccessor`

If a developer introduces one of these dependencies, the architecture test fails, flagging the regression.

## Operational Guidance

- When new features require request-scoped data, pass it explicitly via headers or body payloads rather than persisting it server-side.
- Use authentication tokens (e.g., JWT) or other stateless mechanisms for user identity when that capability is added.
- Keep shared caches (like Redis) reserved for idempotent, explicitly-designed scenarios. Avoid storing per-user session state unless the architecture constraint is intentionally relaxed.
