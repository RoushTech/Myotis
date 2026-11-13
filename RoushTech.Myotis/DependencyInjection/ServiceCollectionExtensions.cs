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
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<MyotisDbContext>(o => o.UseSqlite(configuration.GetConnectionString("Default")));

        // Hosted services start in order, and data protection reads its keys from the database on start.
        services.AddHostedService<DatabaseMigrator>();

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
        services.AddSingleton<CredentialAuthenticator>();
        services.AddSingleton<MailboxMessageStore>();
        services.AddSingleton<PemCertificateFactory>();
        services.AddSingleton<SmtpConnectionInfo>();
        services.AddHostedService<RoleSeeder>();
        services.AddHostedService<UserSeeder>();
        services.AddHostedService<SmtpListener>();
        return services;
    }
}
