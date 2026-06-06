namespace Aspire.Hosting.Upstash.Redis.Management;

internal enum UpstashRedisProviderFailureKind
{
    Validation,
    Authentication,
    Authorization,
    NotFound,
    RateLimited,
    Transient,
    ProviderContract,
    Unexpected,
}
