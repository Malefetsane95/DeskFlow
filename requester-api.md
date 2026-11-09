# Requester ticket API

DeskFlow exposes a read-only API for signed-in End Users. The API uses the existing ASP.NET Core Identity cookie session; it does not issue API tokens. Requests must be authenticated and have the `End User` role.

All endpoints return JSON and use UTC ISO 8601 timestamps. Every ticket query is restricted to the signed-in user's tickets. Requests for another user's ticket return `404 Not Found`, so the endpoint does not reveal whether that ticket exists.

## List tickets

```http
GET /api/requester/tickets?page=1&pageSize=20&statusCode=OPEN&search=printer
```

Query parameters are optional:

| Parameter | Default | Limits |
| --- | --- | --- |
| `page` | `1` | 1 to 1,000,000 |
| `pageSize` | `20` | 1 to 100 |
| `statusCode` | none | Up to 30 characters; matches a ticket status code |
| `search` | none | Up to 100 characters; searches reference, title, and category |

Results are ordered newest first, with ticket ID as a stable tie-breaker. The response includes only tickets owned by the caller.

```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "items": [
    {
      "reference": "DF-26-7QW2MX",
      "title": "Printer is not connecting",
      "category": "Hardware",
      "priority": "Medium",
      "status": "Open",
      "createdAt": "2026-11-09T08:30:00+00:00",
      "updatedAt": "2026-11-09T08:30:00+00:00"
    }
  ]
}
```

## Get ticket details

```http
GET /api/requester/tickets/42
```

The endpoint returns the same ticket fields as the list and its public replies in chronological order:

```json
{
  "reference": "DF-26-7QW2MX",
  "title": "Printer is not connecting",
  "category": "Hardware",
  "priority": "Medium",
  "status": "In progress",
  "createdAt": "2026-11-09T08:30:00+00:00",
  "updatedAt": "2026-11-09T09:15:00+00:00",
  "publicReplies": [
    {
      "author": "IT Support",
      "body": "We are checking the printer connection.",
      "createdAt": "2026-11-09T09:15:00+00:00"
    }
  ]
}
```

## Responses and privacy

- `200 OK` returns a page or ticket DTO.
- `400 Bad Request` means a query parameter is outside its allowed range or length.
- `401 Unauthorized` means the caller is not signed in.
- `403 Forbidden` means the signed-in account does not have the End User role.
- `404 Not Found` means the ticket does not exist or does not belong to the caller.

Responses omit descriptions, email addresses, staff assignments, internal notes, attachment metadata, and storage links. Reply lists include only comments marked public. The API returns DTOs rather than database entities.
