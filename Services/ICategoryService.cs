using EventCalendarWebService.Models;

namespace EventCalendarWebService.Services;

/// <summary>
/// Provides access to category definitions and color mappings.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets all categories with their associated colors and metadata.
    /// </summary>
    /// <returns>A read-only list of category information.</returns>
    IReadOnlyList<CategoryInfo> GetAllCategories();
}
