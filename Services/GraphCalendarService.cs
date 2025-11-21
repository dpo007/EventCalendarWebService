using EventCalendarWebService.Models;
using EventCalendarWebService.Options;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Globalization;

namespace EventCalendarWebService.Services;

public class GraphCalendarService : ICalendarService
{
    private readonly GraphServiceClient _graphClient;
    private readonly GraphApiOptions _options;
    private readonly ILogger<GraphCalendarService> _logger;

    public GraphCalendarService(GraphServiceClient graphClient, IOptions<GraphApiOptions> options, ILogger<GraphCalendarService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
        _options = options.Value;
    }

    public Task<IReadOnlyList<SimpleAppointment>> GetTodaysAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        DateTime start = DateTime.Today;
        DateTime end = DateTime.Today.AddHours(23).AddMinutes(59);
        return GetRangeOfAppointmentsAsync(start, end, cancellationToken);
    }

    public async Task<IReadOnlyList<SimpleAppointment>> GetRangeOfAppointmentsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to the start date.", nameof(endDate));
        }

        IEnumerable<Event> events = await GetCalendarEventsAsync(startDate, endDate, cancellationToken);
        return events.Select(MapAppointment).ToList();
    }

    private async Task<IEnumerable<Event>> GetCalendarEventsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching events for {User} from {Start} to {End} on calendar {Calendar}", _options.CalendarUserUpn, startDate, endDate, _options.CalendarName);

        CalendarCollectionResponse? calendarsResponse = await _graphClient.Users[_options.CalendarUserUpn]
            .Calendars
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Top = 50;
            }, cancellationToken);

        string? calendarId = calendarsResponse?.Value?
            .FirstOrDefault(c => string.Equals(c.Name, _options.CalendarName, StringComparison.OrdinalIgnoreCase))?
            .Id;

        if (string.IsNullOrEmpty(calendarId))
        {
            throw new InvalidOperationException($"Calendar '{_options.CalendarName}' not found for user '{_options.CalendarUserUpn}'.");
        }

        EventCollectionResponse? calendarView = await _graphClient.Users[_options.CalendarUserUpn]
            .Calendars[calendarId]
            .CalendarView
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.StartDateTime = startDate.ToString("o");
                requestConfiguration.QueryParameters.EndDateTime = endDate.ToString("o");
                requestConfiguration.QueryParameters.Top = 255;
                requestConfiguration.QueryParameters.Expand = new[] { "attachments" };
            }, cancellationToken);

        return calendarView?.Value ?? Enumerable.Empty<Event>();
    }

    private SimpleAppointment MapAppointment(Event calendarEvent)
    {
        bool isAllDay = calendarEvent.IsAllDay ?? false;
        DateTime start = ParseGraphDate(calendarEvent.Start, !isAllDay);
        DateTime end = ParseGraphDate(calendarEvent.End, !isAllDay);
        string bodyContent = InlineAttachments(calendarEvent, calendarEvent.Body?.Content ?? string.Empty);

        return new SimpleAppointment
        {
            Id = calendarEvent.Id ?? string.Empty,
            Subject = calendarEvent.Subject ?? string.Empty,
            Body = bodyContent,
            AllDay = isAllDay,
            HtmlColour = GetColouring(calendarEvent),
            Location = calendarEvent.Location?.DisplayName ?? string.Empty,
            Start = ToUnixMilliseconds(start),
            End = ToUnixMilliseconds(end)
        };
    }

    private static DateTime ParseGraphDate(DateTimeTimeZone? dateTime, bool convertToLocal)
    {
        if (dateTime?.DateTime is null)
        {
            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        DateTime parsedDate = DateTime.Parse(dateTime.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        return convertToLocal ? parsedDate.ToLocalTime() : parsedDate;
    }

    private static string InlineAttachments(Event calendarEvent, string bodyContent)
    {
        if (calendarEvent.Attachments is null)
        {
            return bodyContent;
        }

        foreach (Attachment attachment in calendarEvent.Attachments)
        {
            if (attachment is FileAttachment fileAttachment &&
                fileAttachment.IsInline == true &&
                fileAttachment.ContentType?.Contains("image", StringComparison.OrdinalIgnoreCase) == true &&
                fileAttachment.ContentBytes is not null &&
                !string.IsNullOrEmpty(fileAttachment.ContentId))
            {
                string contentId = "cid:" + fileAttachment.ContentId;
                string embeddedImage = $"data:image;base64,{Convert.ToBase64String(fileAttachment.ContentBytes)}";
                bodyContent = bodyContent.Replace(contentId, embeddedImage, StringComparison.OrdinalIgnoreCase);
            }
        }

        return bodyContent;
    }

    private static string GetColouring(Event appointment)
    {
        if (appointment.Categories is null)
        {
            return string.Empty;
        }

        if (appointment.Categories.Contains("Holiday", StringComparer.OrdinalIgnoreCase) || appointment.Categories.Contains("Holidays", StringComparer.OrdinalIgnoreCase))
        {
            return "#41DC6A";
        }

        if (appointment.Categories.Contains("Payday", StringComparer.OrdinalIgnoreCase))
        {
            return "#FBB117";
        }

        if (appointment.Categories.Contains("Community Event", StringComparer.OrdinalIgnoreCase) || appointment.Categories.Contains("Giving Back", StringComparer.OrdinalIgnoreCase))
        {
            return "#D82231";
        }

        if (appointment.Categories.Contains("Webinar", StringComparer.OrdinalIgnoreCase) || appointment.Categories.Contains("Staff Webinar", StringComparer.OrdinalIgnoreCase))
        {
            return "#F47A20";
        }

        return string.Empty;
    }

    private static double ToUnixMilliseconds(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalMilliseconds;
    }
}
