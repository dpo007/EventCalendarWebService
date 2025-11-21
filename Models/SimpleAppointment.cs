namespace EventCalendarWebService.Models;

public class SimpleAppointment
{
    public required string Location { get; init; }

    public required string HtmlColour { get; init; }

    public double End { get; init; }

    public double Start { get; init; }

    public required string Subject { get; init; }

    public required string Body { get; init; }

    public required string Id { get; init; }

    public bool AllDay { get; init; }
}
