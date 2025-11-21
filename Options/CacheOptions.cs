using System.ComponentModel.DataAnnotations;

namespace EventCalendarWebService.Options;

/// <summary>
/// Configuration options for calendar data caching.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Gets or sets the cache duration in minutes for calendar appointments.
    /// </summary>
    [Range(1, 1440)]
    public int DurationMinutes { get; set; } = 5;
}
