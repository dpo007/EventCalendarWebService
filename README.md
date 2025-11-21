# Event Calendar Web Service

A .NET 9 REST API that surfaces calendar events from Microsoft Graph, modernized from the legacy EventCalendarService.

## Endpoints
- `GET /api/appointments` - returns today's appointments.
- `GET /api/appointments?startDate=2025-01-01&endDate=2025-01-07` - returns appointments in the supplied range.
- `GET /health` - basic health probe.

## Configuration
Configure Graph access in `appsettings.json` (or environment variables/user secrets):

```
"GraphApi": {
  "ClientId": "<app-registration-client-id>",
  "TenantId": "<directory-tenant-id>",
  "SecretKey": "<client-secret>",
  "CalendarUserUpn": "calendar-owner@example.com",
  "CalendarName": "Event Calendar"
}
```

The calendar name defaults to **Event Calendar** to match the legacy service.

## Running locally
1. Restore dependencies and build:
   ```
   dotnet restore
   dotnet build
   ```
2. Launch the API:
   ```
   dotnet run
   ```

Use the built-in OpenAPI description in development to explore the endpoints.
