using EventCalendarWebService.Models;
using EventCalendarWebService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCalendarWebService.Controllers;

/// <summary>
/// API controller for retrieving category definitions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesController"/> class.
    /// </summary>
    /// <param name="categoryService">The category service.</param>
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Retrieves all category definitions with their colors and metadata.
    /// </summary>
    /// <returns>A list of all categories including default and custom categories.</returns>
    /// <response code="200">Returns the list of categories.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour since categories rarely change
    public ActionResult<IReadOnlyList<CategoryInfo>> GetAsync()
    {
        IReadOnlyList<CategoryInfo> categories = _categoryService.GetAllCategories();
        return Ok(categories);
    }
}
