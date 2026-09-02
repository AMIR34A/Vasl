using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using Vasl.Domain.Contracts;
using Vasl.Infrastructure.Services.CodeGenerators;

namespace Vasl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICodeGenerator, Base62CodeGenerator>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            return ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!);
        });

        services.AddSingleton<IDistributedLockFactory>(sp =>
        {
            var multiplexers = new List<RedLockMultiplexer>()
            {
                (RedLockMultiplexer)sp.GetRequiredService<IConnectionMultiplexer>()
            };
            return RedLockFactory.Create(multiplexers);
        });

        return services;
    }
}