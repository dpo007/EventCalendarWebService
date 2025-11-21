using EventCalendarWebService.Models;
using EventCalendarWebService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventCalendarWebService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(ICalendarService calendarService, ILogger<AppointmentsController> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SimpleAppointment>>> GetAsync([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken cancellationToken)
    {
        if (!startDate.HasValue && !endDate.HasValue)
        {
            var todaysAppointments = await _calendarService.GetTodaysAppointmentsAsync(cancellationToken);
            return Ok(todaysAppointments);
        }

        if (!startDate.HasValue || !endDate.HasValue)
        {
            return BadRequest("Start date and end date must both be provided.");
        }

        if (endDate.Value < startDate.Value)
        {
            return BadRequest("End date must be greater than or equal to the start date.");
        }

        _logger.LogInformation("Fetching appointments between {Start} and {End}", startDate.Value, endDate.Value);
        var appointments = await _calendarService.GetRangeOfAppointmentsAsync(startDate.Value, endDate.Value, cancellationToken);
        return Ok(appointments);
    }
}
