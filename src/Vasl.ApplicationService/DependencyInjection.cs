using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vasl.ApplicationService;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureApplicationService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(configure =>
        {
            configure.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}