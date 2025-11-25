using EventCalendarWebService.Models;
using EventCalendarWebService.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EventCalendarWebService.Services;

/// <summary>
/// Decorator service that adds memory caching to calendar service operations.
/// </summary>
/// <remarks>
/// Caches calendar events to reduce repeated calls to Microsoft Graph API.
/// Cache duration is configurable via application settings.
/// </remarks>
public class CachedCalendarService : ICalendarService
{
    private readonly ICalendarService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedCalendarService> _logger;
    private readonly TimeSpan _cacheDuration;

    public CachedCalendarService(
        ICalendarService innerService,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions,
        ILogger<CachedCalendarService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
        _cacheDuration = TimeSpan.FromMinutes(cacheOptions.Value.DurationMinutes);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SimpleAppointment>> GetTodaysAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        string cacheKey = GetTodayCacheKey();
        return GetCachedAsync(
            cacheKey,
            () => _innerService.GetTodaysAppointmentsAsync(cancellationToken),
            "Cache miss for today's appointments. Fetching from Graph API. Cache duration: {Duration} minutes.",
            _cacheDuration.TotalMinutes);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SimpleAppointment>> GetRangeOfAppointmentsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = GetRangeCacheKey(startDate, endDate);
        return GetCachedAsync(
            cacheKey,
            () => _innerService.GetRangeOfAppointmentsAsync(startDate, endDate, cancellationToken),
            "Cache miss for appointments from {Start} to {End}. Fetching from Graph API. Cache duration: {Duration} minutes.",
            startDate,
            endDate,
            _cacheDuration.TotalMinutes);
    }

    /// <summary>
    /// Clears all cached calendar data.
    /// </summary>
    public void ClearCache()
    {
        _logger.LogWarning("Cache cleared manually. All cached appointment data will be removed.");

        string todayKey = GetTodayCacheKey();
        _cache.Remove(todayKey);

        _logger.LogInformation("Cleared today's cache key: {CacheKey}. Other date range caches will expire naturally.", todayKey);
    }

    private Task<IReadOnlyList<SimpleAppointment>> GetCachedAsync(
        string cacheKey,
        Func<Task<IReadOnlyList<SimpleAppointment>>> factory,
        string logMessage,
        params object[] logArgs)
    {
        return _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            _logger.LogInformation(logMessage, logArgs);
            return await factory();
        })!;
    }

    private static string GetTodayCacheKey() => $"appointments_today_{DateTime.Today:yyyyMMdd}";

    private static string GetRangeCacheKey(DateTime startDate, DateTime endDate) =>
        $"appointments_range_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
}
