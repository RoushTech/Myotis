using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.DependencyInjection;
using RoushTech.Myotis.Messages;
using RoushTech.Myotis.Settings;
using RoushTech.Myotis.Website.Binders;
using RoushTech.Myotis.Website.Filters;
using RoushTech.Myotis.Website.Hubs;
using RoushTech.Myotis.Website.Mcp;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
var instanceSettings = new InstanceSettingsConfigurationSource();
((IConfigurationBuilder)builder.Configuration).Add(instanceSettings);

var webProxies = (builder.Configuration["Myotis:Web:TrustedProxies"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services
    .AddLogging(opt => { opt.AddSimpleConsole(o => { o.TimestampFormat = "[HH:mm:ss] "; }); })
    .AddSingleton(instanceSettings)
    .AddSingleton<IMailNotifier, SignalRMailNotifier>()
    .AddMyotis(builder.Configuration)
    .AddAuthorization()
    .AddHealthChecks()
    .Services
    .Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
        foreach (var entry in webProxies)
        {
            o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(entry.Contains('/') ? entry : $"{entry}/{(entry.Contains(':') ? 128 : 32)}"));
        }
    })
    .AddScoped<MailboxAccessFilter>()
    .AddSignalR()
    .Services
    .AddControllers(o =>
    {
        o.ModelBinderProviders.Insert(0, new EntityBinderProvider());
        o.Filters.Add<UserErrorExceptionFilter>();
        // Ahead of [ApiController]'s model state check (-2000), which would answer a missing entity with a 400.
        o.Filters.AddService<MailboxAccessFilter>(-3000);
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var mcpJson = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions) { Converters = { new JsonStringEnumConverter() } };
builder.Services
    .AddHttpContextAccessor()
    .AddScoped<McpToolContext>()
    .AddMcpServer(o => o.ServerInstructions =
        "Myotis catches email from test and staging apps so nothing is delivered. Each mailbox collects the mail an app " +
        "sent with that mailbox's SMTP credential. To check an email your action triggered, note the time, take the " +
        "action, then list_messages with since set to that time and read_message to get its links.")
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<MailMcpTools>(mcpJson);

var app = builder.Build();

// With no known proxies the middleware would believe any client, so it only runs when proxies are listed.
if (webProxies.Length > 0)
{
    app.UseForwardedHeaders();
}

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
        endpoints.MapHub<MailHub>("/hubs/mail");
        // Token only: the browser cookie is never honoured on /mcp, so there is no CSRF surface.
        endpoints.MapMcp("/mcp").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = PatAuthenticationDefaults.Scheme });
        // An unmatched API route is a 404, never the SPA shell.
        endpoints.MapFallback("/api/{**rest}", (HttpContext context) =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        endpoints.MapFallbackToFile("index.html");
    });

await app.RunAsync();
