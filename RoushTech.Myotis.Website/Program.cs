using System.Text.Json.Serialization;
using RoushTech.Myotis.DependencyInjection;
using RoushTech.Myotis.Website.Filters;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services
    .AddLogging(opt => { opt.AddSimpleConsole(o => { o.TimestampFormat = "[HH:mm:ss] "; }); })
    .AddMyotis(builder.Configuration)
    .AddAuthorization()
    .AddHealthChecks()
    .Services
    .AddControllers(o => o.Filters.Add<UserErrorExceptionFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app
    .UseDefaultFiles()
    .UseStaticFiles()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapHealthChecks("/health");
        // An unmatched API route is a 404, never the SPA shell.
        endpoints.MapFallback("/api/{**rest}", (HttpContext context) =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        endpoints.MapFallbackToFile("index.html");
    });

await app.RunAsync();
