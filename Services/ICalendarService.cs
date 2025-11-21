using EventCalendarWebService.Models;

namespace EventCalendarWebService.Services;

public interface ICalendarService
{
    Task<IReadOnlyList<SimpleAppointment>> GetTodaysAppointmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SimpleAppointment>> GetRangeOfAppointmentsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
