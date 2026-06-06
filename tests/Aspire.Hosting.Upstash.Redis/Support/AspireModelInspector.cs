#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Upstash.Redis;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Support;

internal static class AspireModelInspector
{
    public static UpstashRedisDeploymentAnnotation GetUpstashAnnotation(RedisResource resource)
    {
        return Assert.Single(resource.Annotations.OfType<UpstashRedisDeploymentAnnotation>());
    }

    public static UpstashRedisDeploymentState GetUpstashState(RedisResource resource)
    {
        return resource.GetUpstashRedisDeploymentState()
            ?? throw new InvalidOperationException("The Redis resource does not have Upstash deployment state.");
    }

    public static int GetPipelineStepCount(RedisResource resource)
    {
        return resource.Annotations.OfType<PipelineStepAnnotation>().Count();
    }
}
