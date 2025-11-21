using Azure.Identity;
using EventCalendarWebService.Options;
using EventCalendarWebService.Services;
using Microsoft.Graph;
using Microsoft.Extensions.Options;

namespace EventCalendarWebService;

public class Program
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddOptions<GraphApiOptions>()
            .Bind(builder.Configuration.GetSection("GraphApi"))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.CalendarName), "Calendar name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.CalendarUserUpn), "Calendar user UPN is required.")
            .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GraphApiOptions>>().Value;
            var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.SecretKey);
            return new GraphServiceClient(credential, GraphScopes);
        });

        builder.Services.AddScoped<ICalendarService, GraphCalendarService>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseCors();

        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        app.Run();
    }
}
