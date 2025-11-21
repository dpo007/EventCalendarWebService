using EventCalendarWebService.Models;
using EventCalendarWebService.Options;
using Microsoft.Extensions.Options;

namespace EventCalendarWebService.Services;

/// <summary>
/// Provides access to category definitions and color mappings.
/// </summary>
/// <remarks>
/// This service combines default categories with custom categories from configuration,
/// providing metadata about which categories are defaults versus custom.
/// </remarks>
public class CategoryService : ICategoryService
{
    private readonly IReadOnlyList<CategoryInfo> _allCategories;

    /// <summary>
    /// Default category color mappings.
    /// </summary>
    private static readonly Dictionary<string, string> DefaultCategoryColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Holiday"] = "#41DC6A",
        ["Holidays"] = "#41DC6A",
        ["Payday"] = "#FBB117",
        ["Community Event"] = "#D82231",
        ["Giving Back"] = "#D82231",
        ["Webinar"] = "#F47A20",
        ["Staff Webinar"] = "#F47A20"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryService"/> class.
    /// </summary>
    /// <param name="categoryOptions">Configuration options for category color mappings.</param>
    public CategoryService(IOptions<CategoryOptions> categoryOptions)
    {
        _allCategories = BuildCategoryList(categoryOptions.Value);
    }

    /// <inheritdoc />
    public IReadOnlyList<CategoryInfo> GetAllCategories()
    {
        return _allCategories;
    }

    /// <summary>
    /// Builds the complete list of categories by merging defaults with custom categories.
    /// </summary>
    /// <param name="categoryOptions">The category options from configuration.</param>
    /// <returns>A read-only list of all categories with metadata.</returns>
    private static IReadOnlyList<CategoryInfo> BuildCategoryList(CategoryOptions categoryOptions)
    {
        List<CategoryInfo> categories = [];
        HashSet<string> processedNames = new(StringComparer.OrdinalIgnoreCase);

        // First, add all custom categories from configuration
        foreach (CategoryDefinition category in categoryOptions.Categories)
        {
            categories.Add(new CategoryInfo
            {
                Name = category.Name,
                HtmlColor = category.HtmlColor,
                IsDefault = false
            });
            processedNames.Add(category.Name);
        }

        // Then, add default categories that haven't been overridden
        foreach (KeyValuePair<string, string> defaultCategory in DefaultCategoryColors)
        {
            if (!processedNames.Contains(defaultCategory.Key))
            {
                categories.Add(new CategoryInfo
                {
                    Name = defaultCategory.Key,
                    HtmlColor = defaultCategory.Value,
                    IsDefault = true
                });
            }
        }

        return categories.AsReadOnly();
    }
}
