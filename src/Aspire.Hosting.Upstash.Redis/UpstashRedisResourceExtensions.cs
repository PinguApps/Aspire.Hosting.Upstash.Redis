using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

internal static class UpstashRedisResourceExtensions
{
    public static UpstashRedisDeploymentState? GetUpstashRedisDeploymentState(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<UpstashRedisDeploymentAnnotation>()
            .SingleOrDefault()
            ?.State;
    }
}
