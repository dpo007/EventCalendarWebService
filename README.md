# Event Calendar Web Service

A .NET 9 REST API that surfaces calendar events from Microsoft Graph, modernized from the legacy EventCalendarService.

## Endpoints
- `GET /api/appointments` - returns today's appointments.
- `GET /api/appointments?startDate=2025-01-01&endDate=2025-01-07` - returns appointments in the supplied range.
- `GET /health` - basic health probe.

## Features
- **Microsoft Graph Integration** - Uses modern Graph SDK with client credentials flow
- **Inline Image Support** - Converts inline email images to base64 data URIs
- **Category-Based Coloring** - Automatic color coding based on appointment categories:
  - Holiday/Holidays: Green (#41DC6A)
  - Payday: Orange (#FBB117)
  - Community Event/Giving Back: Red (#D82231)
  - Webinar/Staff Webinar: Orange (#F47A20)
- **Timezone Handling** - Proper conversion for regular events, preserves dates for all-day events
- **Health Checks** - Built-in health monitoring at `/health`
- **CORS Enabled** - Allows cross-origin requests from any origin (AllowAnyOrigin policy)

## Configuration
Configure Graph access in `appsettings.json` (or environment variables/user secrets):

```json
"GraphApi": {
  "ClientId": "<app-registration-client-id>",
  "TenantId": "<directory-tenant-id>",
  "SecretKey": "<client-secret>",
  "CalendarUserUpn": "calendar-owner@example.com",
  "CalendarName": "Event Calendar"
}
```

### Environment-Specific Configuration
- `appsettings.json` - Base configuration with production-level logging
- `appsettings.Development.json` - Development settings with verbose logging
- `appsettings.Production.json` - Production overrides

## Code Quality
- ✅ **Comprehensive XML documentation** on all public APIs
- ✅ **Inline comments** explaining complex logic
- ✅ **Modern C# patterns** (nullable reference types, init-only properties, pattern matching)
- ✅ **Dependency injection** throughout
- ✅ **Async/await** for all I/O operations
- ✅ **Structured logging** with proper log levels

## Architecture
```
├── Controllers/
│   └── AppointmentsController.cs    # API endpoints
├── Services/
│   ├── ICalendarService.cs          # Service abstraction
│   └── GraphCalendarService.cs      # Graph API implementation
├── Models/
│   └── SimpleAppointment.cs         # DTO for appointments
├── Options/
│   └── GraphApiOptions.cs           # Configuration model
└── Program.cs                        # Application startup
```

## Dependencies
- **Azure.Identity** (1.17.1) - Azure AD authentication
- **Microsoft.Graph** (5.97.0) - Microsoft Graph SDK
- **Microsoft.AspNetCore.OpenApi** (9.0.0) - OpenAPI/Swagger support
