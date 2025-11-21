using EventCalendarWebService.Models;
using EventCalendarWebService.Options;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Globalization;

namespace EventCalendarWebService.Services;

/// <summary>
/// Provides calendar appointment retrieval functionality using Microsoft Graph API.
/// </summary>
/// <remarks>
/// This service authenticates using client credentials and queries a specific user's calendar
/// for events within specified date ranges. It handles inline image attachments and applies
/// category-based color coding.
/// </remarks>
public class GraphCalendarService : ICalendarService
{
    private readonly GraphServiceClient _graphClient;
    private readonly GraphApiOptions _options;
    private readonly ILogger<GraphCalendarService> _logger;

    /// <summary>
    /// Dictionary mapping category names to their corresponding HTML color codes.
    /// Uses case-insensitive comparison for category lookups.
    /// </summary>
    private static readonly Dictionary<string, string> CategoryColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Holiday"] = "#41DC6A",
        ["Holidays"] = "#41DC6A",
        ["Payday"] = "#FBB117",
        ["Community Event"] = "#D82231",
        ["Giving Back"] = "#D82231",
        ["Webinar"] = "#F47A20",
        ["Staff Webinar"] = "#F47A20"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphCalendarService"/> class.
    /// </summary>
    /// <param name="graphClient">The Graph API client for making requests to Microsoft Graph.</param>
    /// <param name="options">Configuration options for Graph API access.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public GraphCalendarService(GraphServiceClient graphClient, IOptions<GraphApiOptions> options, ILogger<GraphCalendarService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SimpleAppointment>> GetTodaysAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        // Set time range to cover the entire current day (00:00:00 to 23:59:00)
        DateTime start = DateTime.Today;
        DateTime end = DateTime.Today.AddHours(23).AddMinutes(59);

        return GetRangeOfAppointmentsAsync(start, end, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SimpleAppointment>> GetRangeOfAppointmentsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // Validate that the date range is logical
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to the start date.", nameof(endDate));
        }

        // Fetch events from Graph API and transform them to our simplified model
        IEnumerable<Event> events = await GetCalendarEventsAsync(startDate, endDate, cancellationToken);
        return events.Select(MapAppointment).ToList();
    }

    /// <summary>
    /// Retrieves calendar events from Microsoft Graph API within the specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enumerable collection of Graph API events.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the specified calendar cannot be found.</exception>
    private async Task<IEnumerable<Event>> GetCalendarEventsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching events for {User} from {Start} to {End} on calendar {Calendar}",
            _options.CalendarUserUpn,
            startDate,
            endDate,
            _options.CalendarName);

        // Step 1: Retrieve the list of calendars for the specified user
        CalendarCollectionResponse? calendarsResponse = await _graphClient.Users[_options.CalendarUserUpn]
            .Calendars
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Top = 50;
            }, cancellationToken);

        // Step 2: Find the calendar by name (case-insensitive comparison)
        string? calendarId = calendarsResponse?.Value?
            .FirstOrDefault(c => string.Equals(c.Name, _options.CalendarName, StringComparison.OrdinalIgnoreCase))?
            .Id;

        if (string.IsNullOrEmpty(calendarId))
        {
            throw new InvalidOperationException($"Calendar '{_options.CalendarName}' not found for user '{_options.CalendarUserUpn}'.");
        }

        // Step 3: Query the calendar view for events in the specified date range
        // Include attachments in the response for inline image processing
        EventCollectionResponse? calendarView = await _graphClient.Users[_options.CalendarUserUpn]
            .Calendars[calendarId]
            .CalendarView
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.StartDateTime = startDate.ToString("o"); // ISO 8601 format
                requestConfiguration.QueryParameters.EndDateTime = endDate.ToString("o");
                requestConfiguration.QueryParameters.Top = 255; // Maximum number of events to return
                requestConfiguration.QueryParameters.Expand = new[] { "attachments" };
            }, cancellationToken);

        return calendarView?.Value ?? Enumerable.Empty<Event>();
    }

    /// <summary>
    /// Maps a Microsoft Graph Event to a simplified SimpleAppointment model.
    /// </summary>
    /// <param name="calendarEvent">The Graph API event to map.</param>
    /// <returns>A simplified appointment object.</returns>
    private SimpleAppointment MapAppointment(Event calendarEvent)
    {
        bool isAllDay = calendarEvent.IsAllDay ?? false;

        // Parse start/end dates, converting to local time for regular events
        DateTime start = ParseGraphDate(calendarEvent.Start, !isAllDay);
        DateTime end = ParseGraphDate(calendarEvent.End, !isAllDay);

        // Process body content to embed inline images as base64 data URIs
        string bodyContent = InlineAttachments(calendarEvent, calendarEvent.Body?.Content ?? string.Empty);

        return new SimpleAppointment
        {
            Id = calendarEvent.Id ?? string.Empty,
            Subject = calendarEvent.Subject ?? string.Empty,
            Body = bodyContent,
            AllDay = isAllDay,
            HtmlColour = GetColouring(calendarEvent),
            Location = calendarEvent.Location?.DisplayName ?? string.Empty,
            Start = ToUnixMilliseconds(start), // Convert to Unix timestamp for JavaScript compatibility
            End = ToUnixMilliseconds(end)
        };
    }

    /// <summary>
    /// Parses a Graph API DateTimeTimeZone object into a .NET DateTime.
    /// </summary>
    /// <param name="dateTime">The Graph API DateTimeTimeZone object.</param>
    /// <param name="convertToLocal">Whether to convert the time to local timezone.</param>
    /// <returns>A DateTime object, or DateTime.MinValue if parsing fails.</returns>
    private static DateTime ParseGraphDate(DateTimeTimeZone? dateTime, bool convertToLocal)
    {
        if (dateTime?.DateTime is null)
        {
            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        // Parse using invariant culture to ensure consistent date parsing
        DateTime parsedDate = DateTime.Parse(dateTime.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        // All-day events should not be converted to local time
        return convertToLocal ? parsedDate.ToLocalTime() : parsedDate;
    }

    /// <summary>
    /// Processes inline image attachments by converting them to base64 data URIs.
    /// </summary>
    /// <param name="calendarEvent">The calendar event containing attachments.</param>
    /// <param name="bodyContent">The HTML body content to process.</param>
    /// <returns>The body content with inline images converted to base64 data URIs.</returns>
    /// <remarks>
    /// This replaces "cid:" content references with embedded base64 image data,
    /// allowing images to display without external requests.
    /// </remarks>
    private static string InlineAttachments(Event calendarEvent, string bodyContent)
    {
        if (calendarEvent.Attachments is null)
        {
            return bodyContent;
        }

        // Process each attachment that is an inline image
        foreach (Attachment attachment in calendarEvent.Attachments)
        {
            if (attachment is FileAttachment fileAttachment &&
                fileAttachment.IsInline == true &&
                fileAttachment.ContentType?.Contains("image", StringComparison.OrdinalIgnoreCase) == true &&
                fileAttachment.ContentBytes is not null &&
                !string.IsNullOrEmpty(fileAttachment.ContentId))
            {
                // Replace the content ID reference (cid:xxx) with a base64 data URI
                string contentId = "cid:" + fileAttachment.ContentId;
                string embeddedImage = $"data:image;base64,{Convert.ToBase64String(fileAttachment.ContentBytes)}";
                bodyContent = bodyContent.Replace(contentId, embeddedImage, StringComparison.OrdinalIgnoreCase);
            }
        }

        return bodyContent;
    }

    /// <summary>
    /// Determines the HTML color code for an appointment based on its categories.
    /// </summary>
    /// <param name="appointment">The calendar event to analyze.</param>
    /// <returns>
    /// An HTML color code if the appointment matches a known category, otherwise an empty string.
    /// </returns>
    /// <remarks>
    /// Supported categories and their colors:
    /// <list type="bullet">
    /// <item><description>Holiday/Holidays: #41DC6A (green)</description></item>
    /// <item><description>Payday: #FBB117 (orange)</description></item>
    /// <item><description>Community Event/Giving Back: #D82231 (red)</description></item>
    /// <item><description>Webinar/Staff Webinar: #F47A20 (orange)</description></item>
    /// </list>
    /// </remarks>
    private static string GetColouring(Event appointment)
    {
        if (appointment.Categories is null)
        {
            return string.Empty;
        }

        // Use dictionary lookup for improved performance
        foreach (string category in appointment.Categories)
        {
            if (CategoryColorMap.TryGetValue(category, out string? color))
            {
                return color;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp in milliseconds.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>The number of milliseconds since Unix epoch (January 1, 1970).</returns>
    /// <remarks>
    /// Unix milliseconds are used for JavaScript Date compatibility.
    /// </remarks>
    private static double ToUnixMilliseconds(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds;
    }
}
