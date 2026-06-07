#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal static class UpstashRedisDeploymentPipeline
{
    public static async Task ExecuteAsync(RedisResource resource, PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(context);

        UpstashRedisDeploymentState state = resource.GetUpstashRedisDeploymentState()
            ?? throw new InvalidOperationException($"Redis resource '{resource.Name}' is missing Upstash deployment state.");

        UpstashRedisResolvedDeployment deployment =
            await UpstashRedisDeployTimeResolver.ResolveAsync(state, resource, context).ConfigureAwait(false);

        using HttpClient httpClient = new();
        UpstashRedisManagementClient client = new(httpClient, deployment.ManagementCredentials);

        _ = await ExecuteAsync(deployment, client, context.CancellationToken).ConfigureAwait(false);
    }

    internal static async Task<UpstashRedisDatabaseDetails?> ExecuteAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(client);

        UpstashRedisOwnershipResolutionResult ownership = await UpstashRedisOwnershipResolver
            .ResolveAsync(
                new UpstashRedisOwnershipResolutionRequest(
                    deployment.DatabaseName,
                    deployment.OwnershipMode,
                    deployment.Options),
                client,
                cancellationToken)
            .ConfigureAwait(false);

        if (ownership.Action != UpstashRedisOwnershipResolutionAction.Adopt)
        {
            return null;
        }

        UpstashRedisDatabaseDetails database = ownership.Database
            ?? throw new InvalidOperationException($"Upstash Redis ownership resolution selected adoption for database '{deployment.DatabaseName}' without returning provider details.");

        return await new UpstashRedisReconciler(client)
            .ReconcileAsync(database, deployment.Options, cancellationToken)
            .ConfigureAwait(false);
    }
}
