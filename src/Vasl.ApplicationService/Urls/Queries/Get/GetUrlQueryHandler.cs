using MediatR;
using Microsoft.EntityFrameworkCore;
using RedLockNet;
using StackExchange.Redis;
using Vasl.Infrastructure.Data;

namespace Vasl.ApplicationService.Urls.Queries.Get;

public class GetUrlQueryHandler : IRequestHandler<GetUrlQuery, GetUrlQueryResponse>
{
    private readonly IDatabase _cache;
    private readonly VaslDbContext _dbContext;
    private readonly IDistributedLockFactory _lockFactory;

    private static string ResourceLock = "Get_Url_Lock";
    private static TimeSpan ExpiryTimeLock = TimeSpan.FromSeconds(2);
    private static TimeSpan WaitTimeLock = TimeSpan.FromSeconds(3);
    private static TimeSpan RetryTimeLock = TimeSpan.FromMicroseconds(500);

    public GetUrlQueryHandler(IConnectionMultiplexer multiplexer, VaslDbContext dbContext, IDistributedLockFactory lockFactory)
    {
        _cache = multiplexer.GetDatabase();
        _dbContext = dbContext;
        _lockFactory = lockFactory;
    }

    public async Task<GetUrlQueryResponse> Handle(GetUrlQuery request, CancellationToken cancellationToken)
    {
        var cacheValue = await _cache.StringGetAsync(new RedisKey(request.Code));

        if (cacheValue.HasValue)
            return new GetUrlQueryResponse(cacheValue!);

        using (var redLock = await _lockFactory.CreateLockAsync(ResourceLock,
            ExpiryTimeLock,
            WaitTimeLock,
            RetryTimeLock,
            cancellationToken))
        {
            if (!redLock.IsAcquired)
                return new GetUrlQueryResponse(string.Empty);

            cacheValue = await _cache.StringGetAsync(new RedisKey(request.Code));

            if (cacheValue.HasValue)
                return new GetUrlQueryResponse(cacheValue!);

            var url = await _dbContext.Urls.FirstOrDefaultAsync(u => u.Code == request.Code, cancellationToken);

            if (url is null || url.IsExpired())
                return new GetUrlQueryResponse(string.Empty);

            await _cache.StringSetAsync(url.Code, url.OriginalUrl, TimeSpan.FromMinutes(30));
            return new GetUrlQueryResponse(url.OriginalUrl);
        }
    }
}