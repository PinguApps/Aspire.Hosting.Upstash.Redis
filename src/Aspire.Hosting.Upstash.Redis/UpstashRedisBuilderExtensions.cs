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
    /// Provides Upstash Redis publishing extensions for a standard Aspire Redis resource.
    /// </summary>
    /// <param name="builder">The existing Redis resource builder returned from <c>AddRedis</c>.</param>
    extension(IResourceBuilder<RedisResource> builder)
    {
        /// <summary>
        /// Marks a standard Aspire Redis resource for Upstash Redis deployment.
        /// </summary>
        /// <param name="databaseName">The explicit remote Upstash Redis database name.</param>
        /// <param name="accountEmail">The infrastructure-only Upstash account email parameter.</param>
        /// <param name="apiKey">The infrastructure-only Upstash API key parameter.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
        /// <param name="configure">Optional Upstash Redis settings to reconcile at deploy time.</param>
        /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="databaseName"/>, <paramref name="accountEmail"/>, or <paramref name="apiKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownershipMode"/> is not a defined ownership mode.</exception>
        /// <exception cref="InvalidOperationException">The configured options contain an unsupported combination.</exception>
        public IResourceBuilder<RedisResource> PublishToUpstash(
            IResourceBuilder<ParameterResource> databaseName,
            IResourceBuilder<ParameterResource> accountEmail,
            IResourceBuilder<ParameterResource> apiKey,
            UpstashRedisOwnershipMode ownershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
            Action<UpstashRedisDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(databaseName);

            return builder.PublishToUpstash(
                UpstashRedisValue.FromParameter(databaseName),
                accountEmail,
                apiKey,
                ownershipMode,
                configure);
        }

        /// <summary>
        /// Marks a standard Aspire Redis resource for Upstash Redis deployment.
        /// </summary>
        /// <param name="databaseName">The explicit remote Upstash Redis database name.</param>
        /// <param name="accountEmail">The infrastructure-only Upstash account email parameter.</param>
        /// <param name="apiKey">The infrastructure-only Upstash API key parameter.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
        /// <param name="configure">Optional Upstash Redis settings to reconcile at deploy time.</param>
        /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="databaseName"/>, <paramref name="accountEmail"/>, or <paramref name="apiKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownershipMode"/> is not a defined ownership mode.</exception>
        /// <exception cref="InvalidOperationException">The configured options contain an unsupported combination.</exception>
        public IResourceBuilder<RedisResource> PublishToUpstash(
            UpstashRedisValue databaseName,
            IResourceBuilder<ParameterResource> accountEmail,
            IResourceBuilder<ParameterResource> apiKey,
            UpstashRedisOwnershipMode ownershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
            Action<UpstashRedisDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(accountEmail);
            ArgumentNullException.ThrowIfNull(apiKey);

            return builder.PublishToUpstash(
                databaseName,
                UpstashRedisValue.FromParameter(accountEmail),
                UpstashRedisValue.FromParameter(apiKey),
                ownershipMode,
                configure);
        }

        /// <summary>
        /// Marks a standard Aspire Redis resource for Upstash Redis deployment.
        /// </summary>
        /// <param name="databaseName">The explicit remote Upstash Redis database name.</param>
        /// <param name="accountEmail">The infrastructure-only Upstash account email value.</param>
        /// <param name="apiKey">The infrastructure-only Upstash API key value.</param>
        /// <param name="ownershipMode">The requested ownership mode for the remote database.</param>
        /// <param name="configure">Optional Upstash Redis settings to reconcile at deploy time.</param>
        /// <returns>The same Redis resource builder for normal Aspire chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="databaseName"/>, <paramref name="accountEmail"/>, or <paramref name="apiKey"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownershipMode"/> is not a defined ownership mode.</exception>
        /// <exception cref="InvalidOperationException">The configured options contain an unsupported combination.</exception>
        public IResourceBuilder<RedisResource> PublishToUpstash(
            UpstashRedisValue databaseName,
            UpstashRedisValue accountEmail,
            UpstashRedisValue apiKey,
            UpstashRedisOwnershipMode ownershipMode = UpstashRedisOwnershipMode.CreateOrAdopt,
            Action<UpstashRedisDeploymentOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(databaseName);
            ArgumentNullException.ThrowIfNull(accountEmail);
            ArgumentNullException.ThrowIfNull(apiKey);

            if (!Enum.IsDefined(ownershipMode))
            {
                throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Upstash Redis ownership mode is not supported.");
            }

            UpstashRedisDeploymentOptions options = new();
            configure?.Invoke(options);
            options.Validate();

            RemoveExistingUpstashPipelineStep(builder.Resource);

            builder.WithAnnotation(
                new UpstashRedisDeploymentAnnotation(
                    databaseName,
                    ownershipMode,
                    accountEmail,
                    apiKey,
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

    private static void RemoveExistingUpstashPipelineStep(RedisResource resource)
    {
        for (int annotationIndex = 0; annotationIndex < resource.Annotations.Count; annotationIndex++)
        {
            if (resource.Annotations[annotationIndex] is not UpstashRedisDeploymentAnnotation)
            {
                continue;
            }

            int pipelineStepAnnotationIndex = annotationIndex + 1;

            if (pipelineStepAnnotationIndex < resource.Annotations.Count
                && resource.Annotations[pipelineStepAnnotationIndex] is PipelineStepAnnotation)
            {
                resource.Annotations.RemoveAt(pipelineStepAnnotationIndex);
            }

            return;
        }
    }
}
