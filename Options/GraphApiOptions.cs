using System.ComponentModel.DataAnnotations;

namespace EventCalendarWebService.Options;

public class GraphApiOptions
{
    private const string DefaultCalendarName = "Event Calendar";

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string TenantId { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string CalendarUserUpn { get; init; } = string.Empty;

    [Required]
    public string CalendarName { get; init; } = DefaultCalendarName;
}
