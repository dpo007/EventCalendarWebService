namespace EventCalendarWebService.Models;

public class SimpleAppointment
{
    public string Location { get; init; }

    public string HtmlColour { get; init; }

    public double End { get; init; }

    public double Start { get; init; }

    public string Subject { get; init; }

    public string Body { get; init; }

    public string Id { get; init; }

    public bool AllDay { get; init; }
}
