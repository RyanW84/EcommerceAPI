# REST Status Code Audit

_Date: 2025-09-25_

This document captures the current status code behavior for the public API surface and highlights how it aligns with the REST guidelines referenced in the Best Practices article.

## Summary of Changes

- Introduced `ControllerResponseExtensions.FromFailure(...)` to ensure non-success flows return the most appropriate HTTP status codes (e.g., `404 Not Found`, `409 Conflict`).
- Updated controllers to rely on the helper so that expected error paths no longer return RFC 7807 problem documents for routine conditions.
- Normalized delete operations across products, categories, and sales to return `204 No Content` on success consistent with REST conventions.

## Endpoint Review

| Resource | Operation | Success Code | Standardized Failure Codes | Notes |
|----------|-----------|--------------|-----------------------------|-------|
| `/api/products` | `GET` (collection) | `200 OK` | `400 Bad Request`, `500 Internal Server Error` | Supports filtering, sorting, pagination with validation errors mapped to 400. |
| `/api/products/{id}` | `GET` | `200 OK` | `404 Not Found`, `500 Internal Server Error` | Returns `404` when entity is missing. |
| `/api/products` | `POST` | `201 Created` | `400 Bad Request`, `409 Conflict`, `500 Internal Server Error` | Location header exposed via `CreatedAtAction`. |
| `/api/products/{id}` | `PUT` | `200 OK` | `400 Bad Request`, `404 Not Found`, `409 Conflict`, `500 Internal Server Error` | Guards against price updates per business rule. |
| `/api/products/{id}` | `DELETE` | `204 No Content` | `404 Not Found`, `409 Conflict`, `500 Internal Server Error` | Soft delete semantics retained; body omitted on success. |
| `/api/categories` | `GET` | `200 OK` | `400 Bad Request`, `500 Internal Server Error` | Includes optional deleted resources when requested. |
| `/api/categories/{id}` | `GET` | `200 OK` | `404 Not Found`, `500 Internal Server Error` | Uses helper to return canonical 404 payload. |
| `/api/categories` | `POST` | `201 Created` | `400 Bad Request`, `409 Conflict` | Name uniqueness violations mapped to `409`. |
| `/api/categories/{id}` | `PUT` | `200 OK` | `400 Bad Request`, `404 Not Found`, `409 Conflict` | Preserves existing relationships. |
| `/api/categories/{id}` | `DELETE` | `204 No Content` | `404 Not Found`, `409 Conflict`, `500 Internal Server Error` | Performs soft delete. |
| `/api/sales` | `GET` | `200 OK` | `400 Bad Request`, `500 Internal Server Error` | Pagination metadata included in response envelope. |
| `/api/sales/{id}` | `GET` | `200 OK` | `404 Not Found`, `500 Internal Server Error` | Leverages helper for consistent failures. |
| `/api/sales` | `POST` | `201 Created` | `400 Bad Request`, `409 Conflict`, `500 Internal Server Error` | Validates inventory; conflicts mapped to `409`. |

## Remaining Considerations

- **Problem Details vs. JSON Body**: We now prefer small JSON error payloads (`{"message": "..."}`) for expected client errors. If the API adopts RFC 7807 problem documents later, the helper can be extended to shape the response accordingly.
- **Validation Details**: Current `400` responses include only a summary message. If richer validation feedback is needed, extend the DTOs or return `ValidationProblem` from controllers when `ModelState` is invalid.
- **Consistency for Optional Endpoints**: Historical sales and restore endpoints follow the same error mapping via the helper, but they can be included in the table when a formal contract is drafted.

Overall, the API now reflects the recommended status codes from the REST best practices article, reducing ambiguity for client integrations.
