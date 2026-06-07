#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.Upstash.Redis;

internal static class UpstashRedisDeploymentPipeline
{
    public static async Task ExecuteAsync(RedisResource resource, PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(context);

        UpstashRedisDeploymentState state = resource.GetUpstashRedisDeploymentState()
            ?? throw new InvalidOperationException($"Redis resource '{resource.Name}' is missing Upstash deployment state.");

        _ = await UpstashRedisDeployTimeResolver.ResolveAsync(state, resource, context).ConfigureAwait(false);
    }
}
