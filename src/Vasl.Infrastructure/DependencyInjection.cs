using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using Vasl.Domain.Contracts;
using Vasl.Infrastructure.Data;
using Vasl.Infrastructure.Services.CodeGenerators;
namespace Vasl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VaslDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Vasl"));
        });

        services.AddSingleton<ICodeGenerator, Base62CodeGenerator>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis")!);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<IDistributedLockFactory>(sp =>
        {
            var multiplexers = new List<RedLockMultiplexer>()
            {
                (RedLockMultiplexer)sp.GetRequiredService<IConnectionMultiplexer>()
            };
            return RedLockFactory.Create(multiplexers);
        });

        services.AddMemoryCache();

        return services;
    }
}