namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal enum UpstashRedisImmutableDriftFailureReason
{
    DatabaseNameMismatch,
    PlatformMismatch,
    PrimaryRegionMismatch,
    TlsDisabled
}
