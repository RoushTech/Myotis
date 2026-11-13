namespace RoushTech.Myotis.DependencyInjection;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoushTech.Myotis.Auth;
using RoushTech.Myotis.Configuration;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;
using RoushTech.Myotis.Mailboxes;
using RoushTech.Myotis.Settings;
using RoushTech.Myotis.Messages;
using RoushTech.Myotis.Smtp;
using RoushTech.Myotis.Users;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyotis(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MyotisOptions>(configuration.GetSection(MyotisOptions.Section));
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.Section))
            .Validate(o => o.Certificate.IsConfigured || !o.Listeners.Any(l => l.NeedsCertificate),
                "Myotis:Smtp:Certificate needs CertificatePath and KeyPath for Implicit and StartTls listeners")
            // The binder silently drops a listener with a value it can't convert, which would quietly turn SMTP off.
            .Validate(o => o.Listeners.Count == configuration.GetSection($"{SmtpOptions.Section}:Listeners").GetChildren().Count(),
                "A listener in Myotis:Smtp:Listeners has a value that isn't valid, such as a port that isn't a number")
            .ValidateOnStart();
        services.Configure<LimitsOptions>(configuration.GetSection(LimitsOptions.Section));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<InstanceSettingsConfigurationSource>();
        services.AddDbContext<MyotisDbContext>(o => o.UseSqlite(configuration.GetConnectionString("Default")));

        // Hosted services start in order, and data protection reads its keys from the database on start.
        services.AddHostedService<DatabaseMigrator>();
        services.AddHostedService<InstanceSettingsLoader>();

        services
            .AddIdentity<User, Role>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<MyotisDbContext>()
            .Services
            .AddDataProtection()
            .PersistKeysToDbContext<MyotisDbContext>()
            .SetApplicationName("RoushTech.Myotis")
            .Services
            .Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1))
            .ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "myotis";
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddScoped<UserService>();
        services.AddScoped<MailboxService>();
        services.AddScoped<SmtpCredentialService>();
        services.AddScoped<MessageIntake>();
        services.AddScoped<MessageReader>();
        services.AddScoped<RetentionService>();
        services.TryAddSingleton<IMailNotifier, NoMailNotifier>();
        services.AddScoped<SettingsService>();
        services.AddSingleton<CredentialAuthenticator>();
        services.AddSingleton<MailboxMessageStore>();
        services.AddSingleton<PemCertificateFactory>();
        services.AddSingleton<SmtpConnectionInfo>();
        services.AddHostedService<RoleSeeder>();
        services.AddHostedService<UserSeeder>();
        services.AddHostedService<SmtpListener>();
        services.AddHostedService<RetentionWorker>();
        return services;
    }
}
