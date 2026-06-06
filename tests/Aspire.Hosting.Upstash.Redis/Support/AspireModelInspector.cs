using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Upstash.Redis;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

internal static class AspireModelInspector
{
    public static UpstashRedisDeploymentAnnotation GetUpstashAnnotation(RedisResource resource)
    {
        return Assert.Single(resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());
    }

    public static int GetPipelineStepCount(RedisResource resource)
    {
        return resource.Annotations.Count(
            annotation => annotation.GetType().FullName == "Aspire.Hosting.Pipelines.PipelineStepAnnotation");
    }
}
