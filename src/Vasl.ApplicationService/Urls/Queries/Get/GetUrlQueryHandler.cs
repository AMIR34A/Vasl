using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Collections.Concurrent;
using Vasl.Infrastructure.Data;

namespace Vasl.ApplicationService.Urls.Queries.Get;

public class GetUrlQueryHandler : IRequestHandler<GetUrlQuery, GetUrlQueryResponse>
{
    private readonly IDatabase _cache;
    private readonly IMemoryCache _localCache;
    private readonly VaslDbContext _dbContext;

    private static readonly ConcurrentDictionary<string, Lazy<Task<GetUrlQueryResponse>>> InFlight = new();

    private const string ShortUrlCachePrefix = "short-url:";

    private static readonly TimeSpan L1Ttl = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PositiveCacheTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NegativeCacheTtl = TimeSpan.FromSeconds(30);

    public const string NotFoundMarker = "__NotFound__";

    public GetUrlQueryHandler(IConnectionMultiplexer multiplexer, IMemoryCache localCache, VaslDbContext dbContext)
    {
        _cache = multiplexer.GetDatabase();
        _localCache = localCache;
        _dbContext = dbContext;
    }

    public async Task<GetUrlQueryResponse> Handle(GetUrlQuery request, CancellationToken cancellationToken)
    {
        var code = request.Code;
        var cacheKey = ShortUrlCachePrefix + code;

        if (_localCache.TryGetValue(cacheKey, out GetUrlQueryResponse? cached))
            return cached!;

        var redisValue = await _cache.StringGetAsync(cacheKey);
        if (redisValue.HasValue)
        {
            var response = redisValue == NotFoundMarker ?
                new GetUrlQueryResponse(NotFoundMarker) :
                new GetUrlQueryResponse(redisValue!);

            _localCache.Set(cacheKey, response, L1Ttl);
            return response;
        }

        var lazyTask = InFlight.GetOrAdd(cacheKey, _ => new Lazy<Task<GetUrlQueryResponse>>(
            () => FetchFromDbAndPopulateAsync(code, cacheKey, cancellationToken)));

        try
        {
            var result = await lazyTask.Value;
            _localCache.Set(cacheKey, result, L1Ttl);
            return result;
        }
        finally
        {
            InFlight.TryRemove(cacheKey, out _);
        }
    }

    private async Task<GetUrlQueryResponse> FetchFromDbAndPopulateAsync(
        string code, string cacheKey, CancellationToken ct)
    {
        var url = await _dbContext.Urls
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Code == code, ct);

        if (url is null || url.IsExpired())
        {
            await _cache.StringSetAsync(cacheKey, NotFoundMarker, NegativeCacheTtl);
            return new GetUrlQueryResponse(NotFoundMarker);
        }

        await _cache.StringSetAsync(cacheKey, url.OriginalUrl, PositiveCacheTtl);
        return new GetUrlQueryResponse(url.OriginalUrl);
    }
}