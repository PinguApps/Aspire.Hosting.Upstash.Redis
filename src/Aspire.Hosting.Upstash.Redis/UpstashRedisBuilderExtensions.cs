#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.Upstash.Redis;

/// <summary>
/// Provides Upstash Redis publishing extensions for Aspire Redis resources.
/// </summary>
public static class UpstashRedisBuilderExtensions
{
    /// <summary>
    /// Marks a standard Aspire Redis resource for Upstash Redis deployment.
    /// </summary>
    /// <param name="builder">The existing Redis resource builder returned from <c>AddRedis</c>.</param>
    /// <param name="databaseName">The explicit remote Upstash Redis database name.</param>
    /// <param name="accountEmail">The infrastructure-only Upstash account email parameter.</param>
    /// <param name="apiKey">The infrastructure-only Upstash API key parameter.</param>
    /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
    /// <param name="configure">Optional Upstash Redis settings to reconcile at deploy time.</param>
    /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
    public static IResourceBuilder<RedisResource> PublishToUpstash(
        this IResourceBuilder<RedisResource> builder,
        string databaseName,
        IResourceBuilder<ParameterResource> accountEmail,
        IResourceBuilder<ParameterResource> apiKey,
        UpstashRedisOwnershipMode ownershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
        Action<UpstashRedisDeploymentOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(accountEmail);
        ArgumentNullException.ThrowIfNull(apiKey);

        UpstashRedisDeploymentOptions options = new();
        configure?.Invoke(options);

        builder.WithAnnotation(
            new UpstashRedisDeploymentAnnotation(
                databaseName,
                ownershipMode,
                accountEmail.Resource,
                apiKey.Resource,
                options),
            ResourceAnnotationMutationBehavior.Replace);

        return builder.WithPipelineStepFactory(
            $"upstash-redis-{builder.Resource.Name}",
            static _ => Task.CompletedTask,
            dependsOn: [WellKnownPipelineSteps.DeployPrereq],
            requiredBy: [WellKnownPipelineSteps.Deploy],
            tags: [WellKnownPipelineTags.ProvisionInfrastructure],
            description: "Provision or reconcile the Upstash Redis database.");
    }
}
