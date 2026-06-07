using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Provides accessors for Upstash Redis metadata attached to Aspire Redis resources.
/// </summary>
public static class UpstashRedisResourceExtensions
{
    /// <summary>
    /// Gets the supplementary app-facing outputs for a Redis resource marked with <c>PublishToUpstash</c>.
    /// </summary>
    /// <param name="resource">The Redis resource.</param>
    /// <returns>The stable Upstash Redis output references.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resource"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The Redis resource has not been marked for Upstash publishing.</exception>
    public static UpstashRedisOutputs GetUpstashRedisOutputs(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<UpstashRedisOutputsAnnotation>()
            .SingleOrDefault()
            ?.Outputs
            ?? throw new InvalidOperationException($"Redis resource '{resource.Name}' has not been marked for Upstash publishing.");
    }

    internal static UpstashRedisDeploymentState? GetUpstashRedisDeploymentState(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<UpstashRedisDeploymentAnnotation>()
            .SingleOrDefault()
            ?.State;
    }

    internal static UpstashRedisOutputs? TryGetUpstashRedisOutputs(this RedisResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations
            .OfType<UpstashRedisOutputsAnnotation>()
            .SingleOrDefault()
            ?.Outputs;
    }
}
