using EventCalendarWebService.Models;

namespace EventCalendarWebService.Services;

/// <summary>
/// Defines a service for retrieving calendar appointments from a calendar provider.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Retrieves all appointments for the current day.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of appointments occurring today.</returns>
    Task<IReadOnlyList<SimpleAppointment>> GetTodaysAppointmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves appointments within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range (inclusive).</param>
    /// <param name="endDate">The end date of the range (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of appointments within the specified date range.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endDate"/> is before <paramref name="startDate"/>.</exception>
    Task<IReadOnlyList<SimpleAppointment>> GetRangeOfAppointmentsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
