namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisOwnershipResolutionException : Exception
{
    public UpstashRedisOwnershipResolutionException()
    {
    }

    public UpstashRedisOwnershipResolutionException(string message)
        : base(message)
    {
    }

    public UpstashRedisOwnershipResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UpstashRedisOwnershipResolutionException(
        UpstashRedisOwnershipResolutionFailureReason failureReason,
        string message)
        : base(message)
    {
        FailureReason = failureReason;
    }

    public UpstashRedisOwnershipResolutionFailureReason FailureReason { get; } = UpstashRedisOwnershipResolutionFailureReason.Unknown;
}
