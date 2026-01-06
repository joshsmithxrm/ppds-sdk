using Microsoft.Extensions.DependencyInjection;
using PPDS.Cli.Services.Query;

namespace PPDS.Cli.Services;

/// <summary>
/// Extension methods for registering CLI application services.
/// </summary>
/// <remarks>
/// Application services encapsulate business logic shared between
/// CLI commands, TUI wizards, and daemon RPC handlers.
/// See ADR-0015 for architectural context.
/// </remarks>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers CLI application services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCliApplicationServices(this IServiceCollection services)
    {
        // Query services
        services.AddTransient<ISqlQueryService, SqlQueryService>();

        // Future services will be registered here:
        // services.AddTransient<IAuthProfileService, AuthProfileService>();
        // services.AddTransient<IEnvironmentService, EnvironmentService>();

        return services;
    }
}
