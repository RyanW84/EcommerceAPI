# HTTP Cache Strategy

_Date: 2025-09-28_

## Goals

- Encourage clients, CDNs, and proxies to reuse safe GET responses.
- Prevent intermediaries from caching mutation results.
- Keep cache metadata discoverable and enforced through automated tests.

## Implementation Summary

1. **Response Caching Services** – `Program.cs` registers `AddResponseCaching()` and the middleware `UseResponseCaching()` so ASP.NET emits the correct cache headers and honours HTTP caching semantics.
2. **Per-endpoint Metadata** – Controllers attach `[ResponseCache]` attributes:
   - Collection and lookup GET endpoints specify `Duration`, `Location`, and `VaryByQueryKeys` (for parameterised queries) to allow public caching for 30–120 seconds.
   - Mutating endpoints (POST/PUT/DELETE, restore) declare `NoStore = true` and `Location = ResponseCacheLocation.None` to prevent caches from persisting state-changing responses.
3. **Automated Guardrails** – `tests/ArchitectureTests/CacheStrategyTests.cs` verifies:
   - Every `HttpGet` action advertises a `ResponseCache` attribute.
   - All mutating actions explicitly disable caching.

## Default Durations

| Endpoint Type | Duration | Notes |
|---------------|----------|-------|
| Product and Category listings | 60–120 seconds | Vary by query keys to keep filters distinct. |
| Sales listings | 30 seconds | Sales data changes frequently; keep the cache short. |
| Entity lookups | 60–120 seconds | Individual records seldom change but are invalidated quickly by mutations. |
| Deleted/restored resource listings | 30 seconds | Low TTL to reflect administrative changes. |
| Mutations | No store | Prevent cached 200/201 responses from being replayed. |

## Operational Guidance

- Adjust `Duration` values as usage patterns emerge; the automated tests only require the attribute to be present (for GET) or to disable caching (for mutations).
- For tenant-specific caching, consider adding `ResponseCacheAttribute.VaryByHeader` (e.g., `Authorization`) once authenticated scenarios are introduced.
- If richer caching behaviour is required (ETags, conditional requests), layer it on top of the current baseline while keeping controller attributes in sync.
