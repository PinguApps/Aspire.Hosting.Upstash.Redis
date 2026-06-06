using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisDeploymentAnnotation : IResourceAnnotation
{
    public UpstashRedisDeploymentAnnotation(
        UpstashRedisValue databaseName,
        UpstashRedisOwnershipMode ownershipMode,
        UpstashRedisValue accountEmail,
        UpstashRedisValue apiKey,
        UpstashRedisDeploymentOptions options)
    {
        State = new UpstashRedisDeploymentState(
            databaseName,
            ownershipMode,
            accountEmail,
            apiKey,
            options);
    }

    public UpstashRedisDeploymentState State
    {
        get;
    }
}
