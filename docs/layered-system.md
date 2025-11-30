# Layered System Support

_Date: 2025-09-28_

## Objectives

- Let reverse proxies and gateways sit transparently in front of the API without breaking scheme/host detection.
- Keep application layers (controllers → services → repositories) isolated so cross-layer coupling does not leak implementation details.
- Provide automated checks that surface regressions when layering rules are violated.

## Implementation Summary

1. **Forwarded Headers Middleware**
   - `Program.cs` registers and enables `ForwardedHeadersOptions`, clearing the trusted network lists so reverse proxies (NGINX, Traefik, Azure Front Door, etc.) can forward `X-Forwarded-*` headers.
   - `app.UseForwardedHeaders()` runs before HTTPS redirection and routing to ensure downstream middleware sees the original client protocol/host.

2. **Layering Guardrails**
   - `tests/ArchitectureTests/LayeredArchitectureTests.cs` validates:
     - Controllers do **not** depend directly on repositories or the EF Core data layer.
     - Services and repositories remain independent from controller/UI concerns.
   - Existing client/server separation and statelessness tests supplement these rules, reinforcing the layered design.

3. **Operational Notes**
   - For locked-down deployments, configure `ForwardedHeadersOptions.KnownIPNetworks` / `KnownProxies` via appsettings to whitelist ingress proxies.
   - When adding new layers (e.g., background workers, gRPC, GraphQL), replicate the same isolation: expose only interfaces to the next layer out.
   - Combine with the REST cache strategy to place CDN/proxy caches in front of the API without modifying the API implementation.

## Validation Steps

- Run `dotnet test` to execute the architecture suite and ensure layering rules remain intact.
- When deploying behind reverse proxies, verify that generated absolute URLs (e.g., from `CreatedAtAction`) reflect the original scheme/host—`UseForwardedHeaders` makes this automatic.
