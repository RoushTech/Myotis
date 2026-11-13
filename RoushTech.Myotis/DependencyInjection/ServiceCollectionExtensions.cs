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
using RoushTech.Myotis.Users;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyotis(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MyotisOptions>(configuration.GetSection(MyotisOptions.Section));
        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<MyotisDbContext>(o => o.UseSqlite(configuration.GetConnectionString("Default")));

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
        services.AddHostedService<DatabaseMigrator>();
        services.AddHostedService<RoleSeeder>();
        services.AddHostedService<UserSeeder>();
        return services;
    }
}
