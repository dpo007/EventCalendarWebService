using Azure.Identity;
using EventCalendarWebService.Options;
using EventCalendarWebService.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace EventCalendarWebService;

/// <summary>
/// The main entry point for the Event Calendar Web Service application.
/// </summary>
public class Program
{
    /// <summary>
    /// The Graph API scopes required for calendar access.
    /// </summary>
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Configure and validate Graph API options from appsettings
        builder.Services
            .AddOptions<GraphApiOptions>()
            .Bind(builder.Configuration.GetSection("GraphApi"))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.CalendarName), "Calendar name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.CalendarUserUpn), "Calendar user UPN is required.")
            .ValidateOnStart();

        // Configure cache options from appsettings
        builder.Services
            .AddOptions<CacheOptions>()
            .Bind(builder.Configuration.GetSection("Cache"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register ClientSecretCredential as singleton for token caching
        builder.Services.AddSingleton<ClientSecretCredential>(sp =>
        {
            GraphApiOptions options = sp.GetRequiredService<IOptions<GraphApiOptions>>().Value;
            return new ClientSecretCredential(options.TenantId, options.ClientId, options.SecretKey);
        });

        // Register Graph Service Client with shared credential instance
        builder.Services.AddSingleton(sp =>
        {
            ClientSecretCredential credential = sp.GetRequiredService<ClientSecretCredential>();
            return new GraphServiceClient(credential, GraphScopes);
        });

        // Register application services with caching
        builder.Services.AddSingleton<GraphCalendarService>();
        builder.Services.AddSingleton<ICalendarService>(sp =>
        {
            GraphCalendarService innerService = sp.GetRequiredService<GraphCalendarService>();
            IMemoryCache cache = sp.GetRequiredService<IMemoryCache>();
            IOptions<CacheOptions> cacheOptions = sp.GetRequiredService<IOptions<CacheOptions>>();
            ILogger<CachedCalendarService> logger = sp.GetRequiredService<ILogger<CachedCalendarService>>();
            return new CachedCalendarService(innerService, cache, cacheOptions, logger);
        });

        // Configure CORS to allow any origin (adjust for production as needed)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        // Add framework services
        builder.Services.AddControllers(options =>
        {
            // Configure response cache profile using cache duration from settings
            int cacheDurationSeconds = builder.Configuration.GetSection("Cache").GetValue<int>("DurationMinutes", 5) * 60;
            options.CacheProfiles.Add("Default", new Microsoft.AspNetCore.Mvc.CacheProfile
            {
                Duration = cacheDurationSeconds,
                VaryByQueryKeys = ["startDate", "endDate"]
            });
        });
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();
        builder.Services.AddMemoryCache();
        builder.Services.AddResponseCaching();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseResponseCaching();
        app.UseCors();
        app.UseAuthorization();

        // Map endpoints
        app.MapControllers();
        app.MapHealthChecks("/health");

        app.Run();
    }
}
