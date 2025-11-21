namespace EventCalendarWebService.Options;

/// <summary>
/// Configuration options for category color mappings.
/// </summary>
/// <remarks>
/// Categories defined in this configuration will be merged with any default categories
/// to determine HTML color codes for calendar appointments.
/// </remarks>
public class CategoryOptions
{
    /// <summary>
    /// Gets or sets the list of category definitions.
    /// </summary>
    public List<CategoryDefinition> Categories { get; set; } = [];
}

/// <summary>
/// Represents a category with its associated HTML color code.
/// </summary>
public class CategoryDefinition
{
    /// <summary>
    /// Gets or sets the category name (case-insensitive matching).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the HTML color code (e.g., "#41DC6A").
    /// </summary>
    public required string HtmlColor { get; set; }
}
