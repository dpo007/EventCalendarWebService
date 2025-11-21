namespace EventCalendarWebService.Models;

/// <summary>
/// Represents a simplified calendar appointment with essential details.
/// </summary>
/// <remarks>
/// This model is used to return appointment data from the API.
/// Timestamps are represented as Unix milliseconds for JavaScript compatibility.
/// </remarks>
public class SimpleAppointment
{
    /// <summary>
    /// Gets or initializes the unique identifier for the appointment.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or initializes the subject/title of the appointment.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets or initializes the HTML body content of the appointment.
    /// May contain inline base64-encoded images.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Gets or initializes the location where the appointment takes place.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets or initializes the HTML color code for the appointment based on its category.
    /// </summary>
    /// <remarks>
    /// Color is determined by appointment categories using mappings defined in Categories.json.
    /// Default categories include:
    /// - Holiday/Holidays: #41DC6A (green)
    /// - Payday: #FBB117 (orange)
    /// - Community Event/Giving Back: #D82231 (red)
    /// - Webinar/Staff Webinar: #F47A20 (orange)
    /// Custom categories can be added or overridden via the Categories.json configuration file.
    /// </remarks>
    public string? HtmlColour { get; init; }

    /// <summary>
    /// Gets or initializes the start time of the appointment as Unix timestamp in milliseconds.
    /// </summary>
    public double Start { get; init; }

    /// <summary>
    /// Gets or initializes the end time of the appointment as Unix timestamp in milliseconds.
    /// </summary>
    public double End { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether this is an all-day event.
    /// </summary>
    /// <remarks>
    /// All-day events are not converted to local time.
    /// </remarks>
    public bool AllDay { get; init; }
}
