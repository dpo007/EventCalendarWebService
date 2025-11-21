using EventCalendarWebService.Models;
using EventCalendarWebService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCalendarWebService.Controllers;

/// <summary>
/// API controller for retrieving calendar appointments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<AppointmentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppointmentsController"/> class.
    /// </summary>
    public AppointmentsController(ICalendarService calendarService, ILogger<AppointmentsController> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves appointments for today or within a specified date range.
    /// </summary>
    /// <param name="startDate">Optional start date for the range query. If omitted with endDate, returns today's appointments.</param>
    /// <param name="endDate">Optional end date for the range query. Must be provided if startDate is specified.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A list of appointments matching the query criteria.</returns>
    /// <response code="200">Returns the list of appointments.</response>
    /// <response code="400">If the date parameters are invalid or incomplete.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SimpleAppointment>>> GetAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        // If no dates provided, return today's appointments
        if (!startDate.HasValue && !endDate.HasValue)
        {
            IReadOnlyList<SimpleAppointment> todaysAppointments = await _calendarService.GetTodaysAppointmentsAsync(cancellationToken);
            return Ok(todaysAppointments);
        }

        // Both dates must be provided together
        if (!startDate.HasValue || !endDate.HasValue)
        {
            return BadRequest("Start date and end date must both be provided.");
        }

        // Validate date range logic
        if (endDate.Value < startDate.Value)
        {
            return BadRequest("End date must be greater than or equal to the start date.");
        }

        // Fetch appointments for the specified range
        _logger.LogInformation("Fetching appointments between {Start} and {End}", startDate.Value, endDate.Value);
        IReadOnlyList<SimpleAppointment> appointments = await _calendarService.GetRangeOfAppointmentsAsync(startDate.Value, endDate.Value, cancellationToken);

        return Ok(appointments);
    }
}
