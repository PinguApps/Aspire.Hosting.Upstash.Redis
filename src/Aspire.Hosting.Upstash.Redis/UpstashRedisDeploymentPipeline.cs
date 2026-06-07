#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Microsoft.Extensions.DependencyInjection;

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

        UpstashRedisRemoteIdentityDeploymentStateStore identityStateStore = new(
            context.Services.GetRequiredService<IDeploymentStateManager>());
        UpstashRedisRemoteIdentityState? cachedIdentity =
            await identityStateStore.LoadAsync(resource.Name, context.CancellationToken).ConfigureAwait(false);

        _ = await ExecuteAsync(
            deployment,
            client,
            cachedIdentity,
            identityState => identityStateStore.SaveAsync(resource.Name, identityState, context.CancellationToken),
            context.CancellationToken).ConfigureAwait(false);
    }

    internal static async Task<UpstashRedisDatabaseDetails?> ExecuteAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            deployment,
            client,
            cachedIdentity: null,
            saveIdentityStateAsync: null,
            cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<UpstashRedisDatabaseDetails?> ExecuteAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        UpstashRedisRemoteIdentityState? cachedIdentity,
        Func<UpstashRedisRemoteIdentityState, Task>? saveIdentityStateAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(client);

        UpstashRedisRemoteIdentityResolution identity = await new UpstashRedisRemoteIdentityResolver(client)
            .ResolveAsync(deployment.DatabaseName, cachedIdentity, cancellationToken)
            .ConfigureAwait(false);

        UpstashRedisOwnershipResolutionResult ownership = await UpstashRedisOwnershipResolver
            .ResolveAsync(
                new UpstashRedisOwnershipResolutionRequest(
                    deployment.DatabaseName,
                    deployment.OwnershipMode,
                    deployment.Options,
                    identity.Database),
                client,
                cancellationToken)
            .ConfigureAwait(false);

        if (ownership.Action != UpstashRedisOwnershipResolutionAction.Adopt)
        {
            return null;
        }

        UpstashRedisDatabaseDetails database = ownership.Database
            ?? throw new InvalidOperationException($"Upstash Redis ownership resolution selected adoption for database '{deployment.DatabaseName}' without returning provider details.");

        UpstashRedisDatabaseDetails reconciledDatabase = await new UpstashRedisReconciler(client)
            .ReconcileAsync(database, deployment.Options, cancellationToken)
            .ConfigureAwait(false);

        if (identity.IdentityState is not null && saveIdentityStateAsync is not null)
        {
            await saveIdentityStateAsync(identity.IdentityState).ConfigureAwait(false);
        }

        return reconciledDatabase;
    }
}
