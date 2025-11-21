using System.ComponentModel.DataAnnotations;

namespace EventCalendarWebService.Options;

/// <summary>
/// Configuration options for Microsoft Graph API authentication and calendar access.
/// </summary>
/// <remarks>
/// These options are typically bound from the "GraphApi" section in appsettings.json.
/// All properties are required and validated on application startup.
/// </remarks>
public class GraphApiOptions
{
    private const string DefaultCalendarName = "Event Calendar";

    /// <summary>
    /// Gets or initializes the Azure AD application (client) ID.
    /// </summary>
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the Azure AD directory (tenant) ID.
    /// </summary>
    [Required]
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the client secret for the Azure AD application.
    /// </summary>
    /// <remarks>
    /// This should be stored securely using Azure Key Vault, user secrets, or environment variables in production.
    /// </remarks>
    [Required]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the User Principal Name (UPN) of the calendar owner.
    /// </summary>
    /// <example>calendar-owner@example.com</example>
    [Required]
    public string CalendarUserUpn { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the name of the calendar to query.
    /// </summary>
    /// <remarks>
    /// Defaults to "Event Calendar" if not specified.
    /// </remarks>
    [Required]
    public string CalendarName { get; init; } = DefaultCalendarName;
}
