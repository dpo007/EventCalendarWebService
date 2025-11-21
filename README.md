# Event Calendar Web Service

A .NET 9 REST API that surfaces calendar events from Microsoft Graph, providing a streamlined, high-performance interface for accessing organizational calendar data.

## Overview

This web service acts as a middleware layer between client applications and Microsoft Graph, simplifying calendar access with a clean REST API. It's designed for scenarios where you need to display organizational calendar events (such as company holidays, paydays, community events, or webinars) in internal portals, dashboards, or custom applications without implementing the full Microsoft Graph authentication flow on the client side.

**Key Benefits:**
- **Simplified Integration** - Single REST endpoint instead of complex Graph API queries
- **High Performance** - Multi-layer caching drastically reduces API calls and improves response times
- **Centralized Authentication** - Service-to-service authentication using client credentials (no user login required)
- **Pre-processed Data** - Events are enriched with color coding, inline images, and timezone conversions
- **Customizable Categories** - Define custom category colors without code changes
- **Frontend-Friendly** - Returns data in a simplified format optimized for JavaScript/web consumption
- **Production-Ready** - Includes health checks, structured logging, configurable caching, and comprehensive error handling

## Use Cases
- Display company-wide events on employee intranet dashboards
- Integrate organizational calendar into custom web applications
- Provide read-only calendar access to third-party systems
- Create public-facing event calendars from private Microsoft 365 calendars
- Build notification systems for upcoming events

## Endpoints

### Calendar Data
- `GET /api/appointments` - Returns today's appointments
- `GET /api/appointments?startDate=2025-01-01&endDate=2025-01-07` - Returns appointments in the supplied date range

### Category Configuration
- `GET /api/categories` - Returns all configured categories with their colors and metadata (includes both default and custom categories)

### Cache Management
- `GET /api/appointments/cache/clear` - Clears all cached appointment data (forces fresh data on next request)

### Health
- `GET /health` - Basic health probe

## Features

### Core Functionality
- **Microsoft Graph Integration** - Uses modern Graph SDK with client credentials flow
- **Inline Image Support** - Converts inline email images to base64 data URIs
- **Customizable Category-Based Coloring** - Automatic color coding based on appointment categories with configurable colors via JSON
- **Timezone Handling** - Proper conversion for regular events, preserves dates for all-day events

### Performance Optimizations
- **Multi-Layer Caching Strategy**:
  - **HTTP Response Caching** - Client/browser-side caching (configurable duration)
  - **Server Memory Cache** - In-memory caching of appointment data (configurable duration)
  - **Calendar ID Caching** - Cached calendar lookups for lifetime of the service
  - **Azure AD Token Caching** - Automatic token reuse via singleton credential
- **Singleton Services** - All services registered as singletons for maximum cache effectiveness
- **Configurable Cache Duration** - Adjust cache timing via appsettings.json (default: 5 minutes)
- **Manual Cache Refresh** - Force immediate cache invalidation via API endpoint

### Operations
- **Health Checks** - Built-in health monitoring at `/health`
- **CORS Enabled** - Allows cross-origin requests from any origin (configurable)
- **Structured Logging** - Cache hits/misses and performance metrics logged

## Configuration

### Main Application Configuration

Configure the service in `appsettings.json` (or environment variables/user secrets):

```json
{
  "GraphApi": {
    "ClientId": "<app-registration-client-id>",
    "TenantId": "<directory-tenant-id>",
    "SecretKey": "<client-secret>",
    "CalendarUserUpn": "calendar-owner@example.com",
    "CalendarName": "Event Calendar"
  },
  "Cache": {
    "DurationMinutes": 5
  }
}
```

#### GraphApi Section
- `ClientId` - Azure AD app registration client ID
- `TenantId` - Azure AD directory (tenant) ID
- `SecretKey` - Client secret from app registration
- `CalendarUserUpn` - User principal name of the calendar owner
- `CalendarName` - Name of the specific calendar to query

#### Cache Section
- `DurationMinutes` - Cache duration in minutes (default: 5, range: 1-1440)
  - Applies to both server-side memory cache and HTTP response cache
  - Lower values = fresher data, higher API usage
  - Higher values = better performance, potentially stale data

### Category Color Customization

Customize appointment colors by creating a `Categories.json` file in the application root directory (same location as `appsettings.json`):

```json
{
  "Categories": [
    {
      "Name": "Holiday",
      "HtmlColor": "#41DC6A"
    },
    {
      "Name": "Custom Category",
      "HtmlColor": "#FF5733"
    }
  ]
}
```

#### Default Categories

The service includes these built-in category colors:
- **Holiday/Holidays**: `#41DC6A` (green)
- **Payday**: `#FBB117` (orange)
- **Community Event/Giving Back**: `#D82231` (red)
- **Webinar/Staff Webinar**: `#F47A20` (orange)

#### How Category Colors Work

1. **Default Behavior**: If `Categories.json` is missing or empty, the service uses the built-in default categories
2. **Add Custom Categories**: Define new categories in `Categories.json` - they will be merged with the defaults
3. **Override Default Colors**: Define a category with the same name to override its color
4. **Case-Insensitive Matching**: Category names are matched case-insensitively (e.g., "holiday" matches "Holiday")
5. **Hot Reload**: Changes to `Categories.json` are detected automatically (no restart required)

> **Tip**: Use `GET /api/categories` to view the current merged list of all configured categories and verify your customizations.

#### Example Scenarios

**Scenario 1: Add a new custom category**
```json
{
  "Categories": [
    {
      "Name": "Training",
      "HtmlColor": "#9B59B6"
    }
  ]
}
```
Result: All default categories still work + new "Training" category with purple color

**Scenario 2: Override an existing category color**
```json
{
  "Categories": [
    {
      "Name": "Payday",
      "HtmlColor": "#00FF00"
    }
  ]
}
```
Result: Payday events now use bright green instead of orange, all other defaults unchanged

**Scenario 3: Use only default categories**
```json
{
  "Categories": []
}
```
Or simply delete/don't create `Categories.json` - Result: All default categories work as normal

### Environment-Specific Configuration
- `appsettings.json` - Base configuration with production-level logging
- `appsettings.Development.json` - Development settings with verbose logging
- `appsettings.Production.json` - Production overrides
- `Categories.json` - Category color customization (optional, applies to all environments)

## Performance Characteristics

With default 5-minute caching:
- **First request**: ~200-500ms (Graph API call)
- **Cached requests**: <1ms (memory lookup)
- **Expected cache hit ratio**: 80-95% for typical usage
- **Calendar updates visible**: Within 5 minutes (or immediately after cache clear)
- **API call reduction**: 95%+ reduction in Microsoft Graph API calls

## Usage Examples

### Get Today's Appointments
```bash
curl https://your-api/api/appointments
```

### Get Appointments for Date Range
```bash
curl "https://your-api/api/appointments?startDate=2025-01-01&endDate=2025-01-31"
```

### Get All Configured Categories
```bash
curl https://your-api/api/categories
```

### Force Cache Refresh
```bash
curl https://your-api/api/appointments/cache/clear
```

### Example Response - Appointments
```json
{
        "id": "AAMkAGI...",
        "subject": "Holiday - New Year's Day",
        "body": null,
        "location": null,
        "htmlColour": "#41DC6A",
        "start": 1704067200000,
        "end": 1704153600000,
        "allDay": true
      }
```
### Example Response - Categories
```json
[
  {
    "name": "Holiday",
    "htmlColor": "#41DC6A",
    "isDefault": true
  },
  {
    "name": "Holidays",
    "htmlColor": "#41DC6A",
    "isDefault": true
  },
  {
    "name": "Payday",
    "htmlColor": "#FBB117",
    "isDefault": true
  },
  {
    "name": "Community Event",
    "htmlColor": "#D82231",
    "isDefault": true
  },
  {
    "name": "Custom Category",
    "htmlColor": "#FF5733",
    "isDefault": false
  }
]
```

**Response Fields:**
- `name` - The category name (case-insensitive matching)
- `htmlColor` - HTML color code (e.g., "#41DC6A")
- `isDefault` - `true` for built-in categories, `false` for custom categories from Categories.json

## Code Quality
- ✅ **Comprehensive XML documentation** on all public APIs
- ✅ **Inline comments** explaining complex logic
- ✅ **Modern C# patterns** (nullable reference types, init-only properties, pattern matching)
- ✅ **Dependency injection** throughout with singleton pattern for performance
- ✅ **Async/await** for all I/O operations
- ✅ **Structured logging** with proper log levels and cache metrics
- ✅ **Decorator pattern** for clean separation of caching concerns

## Architecture
```
├── Controllers/
│   ├── AppointmentsController.cs    # Appointment endpoints + cache management
│   └── CategoriesController.cs      # Category configuration endpoint
├── Services/
│   ├── ICalendarService.cs          # Calendar service abstraction
│   ├── GraphCalendarService.cs      # Graph API implementation
│   ├── CachedCalendarService.cs     # Caching decorator
│   ├── ICategoryService.cs          # Category service abstraction
│   └── CategoryService.cs           # Category management implementation
├── Models/
│   ├── SimpleAppointment.cs         # DTO for appointments
│   └── CategoryInfo.cs              # DTO for category information
├── Options/
│   ├── GraphApiOptions.cs           # Graph API configuration model
│   ├── CacheOptions.cs              # Cache configuration model
│   └── CategoryOptions.cs           # Category color configuration model
├── Program.cs                        # Application startup + DI configuration
├── appsettings.json                  # Main application configuration
└── Categories.json                   # Optional category color customization
```

### Design Patterns
- **Decorator Pattern** - `CachedCalendarService` wraps `GraphCalendarService` for transparent caching
- **Singleton Pattern** - All services registered as singletons for maximum cache effectiveness
- **Options Pattern** - Configuration via strongly-typed options classes with validation
- **Dependency Injection** - Constructor injection throughout

## Dependencies
- **Azure.Identity** (1.17.1) - Azure AD authentication with token caching
- **Microsoft.Graph** (5.97.0) - Microsoft Graph SDK
- **Microsoft.AspNetCore.OpenApi** (9.0.0) - OpenAPI/Swagger support
- **Microsoft.Extensions.Caching.Memory** - Built-in memory caching
