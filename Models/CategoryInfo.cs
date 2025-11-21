namespace EventCalendarWebService.Models;

/// <summary>
/// Represents a category with its color and metadata.
/// </summary>
public class CategoryInfo
{
    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the HTML color code (e.g., "#41DC6A").
    /// </summary>
    public required string HtmlColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a default/static category.
    /// </summary>
    public bool IsDefault { get; set; }
}
