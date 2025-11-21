using Azure.Identity;
using EventCalendarWebService.Options;
using EventCalendarWebService.Services;
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

        // Register application services
        builder.Services.AddSingleton<ICalendarService, GraphCalendarService>();

        // Configure CORS to allow any origin (adjust for production as needed)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        // Add framework services
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors();
        app.UseAuthorization();

        // Map endpoints
        app.MapControllers();
        app.MapHealthChecks("/health");

        app.Run();
    }
}
