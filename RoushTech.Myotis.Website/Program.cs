using System.Text.Json.Serialization;
using RoushTech.Myotis.DependencyInjection;
using RoushTech.Myotis.Website.Binders;
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
    .AddScoped<MailboxAccessFilter>()
    .AddControllers(o =>
    {
        o.ModelBinderProviders.Insert(0, new EntityBinderProvider());
        o.Filters.Add<UserErrorExceptionFilter>();
        // Ahead of [ApiController]'s model state check (-2000), which would answer a missing entity with a 400.
        o.Filters.AddService<MailboxAccessFilter>(-3000);
    })
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
