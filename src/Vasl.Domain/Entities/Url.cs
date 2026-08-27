using Ardalis.GuardClauses;

namespace Vasl.Domain.Entities;

public class Url
{
    public long Id { get; private set; }

    public string Code { get; private set; } = default!;

    public string OriginalUrl { get; private set; } = default!;

    public DateTime CreationTimeUtc { get; private set; }

    public DateTime? ExpirationTimeUtc { get; private set; }

    private Url() { }

    public static Url Create(long id, string code, string url, DateTime? expirationTime)
    {
        DateTime dateTimeUtc = DateTime.UtcNow;

        Guard.Against.NegativeOrZero(id, nameof(id));
        Guard.Against.NullOrEmpty(code, nameof(code));
        Guard.Against.NullOrEmpty(url, nameof(url));

        if (expirationTime.HasValue && expirationTime > dateTimeUtc)
            throw new InvalidDataException(nameof(expirationTime));

        return new Url()
        {
            Id = id,
            Code = code,
            OriginalUrl = url,
            CreationTimeUtc = dateTimeUtc,
            ExpirationTimeUtc = expirationTime
        };
    }

    public bool IsExpired() => ExpirationTimeUtc.HasValue && ExpirationTimeUtc > DateTime.UtcNow;

    #region Equality
    public override bool Equals(object? obj) => obj is Url url && Id.Equals(url.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
    #endregion
}