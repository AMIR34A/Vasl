using Vasl.ApplicationService;
using Vasl.Infrastructure;
using Vasl.WebAPI.Endpoints;

namespace Vasl.WebAPI;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        services.ConfigureInfrastructure(configuration);
        services.ConfigureApplicationService(configuration);

        return services;
    }

    public static void MapEndpoints(this WebApplication app, IConfiguration configuration)
    {
        var acceptableEndpointType = configuration.GetRequiredSection("AppSettings:AcceptableEndpointType").Get<EndpointType>();

        var assembly = typeof(IEndpoint).Assembly;

        var endpoints = assembly.DefinedTypes
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => (IEndpoint)Activator.CreateInstance(t.AsType())!)
            .Where(e => acceptableEndpointType switch
            {
                EndpointType.Read => e.Type == EndpointType.Read,
                EndpointType.Write => e.Type == EndpointType.Write,
                _ => true
            }).ToArray();

        foreach (var endpoint in endpoints)
            endpoint.AddEndpoint(app);
    }
}