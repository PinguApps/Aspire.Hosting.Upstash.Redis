#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Upstash.Redis.Deployment;
using Aspire.Hosting.Upstash.Redis.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Upstash.Redis;

internal static class UpstashRedisDeploymentPipeline
{
    private static readonly HttpClient _managementHttpClient = new()
    {
        BaseAddress = new Uri("https://api.upstash.com/v2/"),
    };

    private static readonly Action<ILogger, string, string, Exception?> _resolvingDatabase =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, "ResolvingDatabase"),
            "Resolving Upstash Redis database '{DatabaseName}' for Redis resource '{ResourceName}'.");

    private static readonly Action<ILogger, string, string, Exception?> _createdDatabase =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, "CreatedDatabase"),
            "Created Upstash Redis database '{DatabaseName}' for Redis resource '{ResourceName}'.");

    private static readonly Action<ILogger, string, string, Exception?> _usingExistingDatabase =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3, "UsingExistingDatabase"),
            "Using existing Upstash Redis database '{DatabaseName}' for Redis resource '{ResourceName}'.");

    public static async Task ExecuteAsync(RedisResource resource, PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(context);

        UpstashRedisDeploymentState state = resource.GetUpstashRedisDeploymentState()
            ?? throw new InvalidOperationException($"Redis resource '{resource.Name}' is missing Upstash deployment state.");

        UpstashRedisResolvedDeployment deployment =
            await UpstashRedisDeployTimeResolver.ResolveAsync(state, resource, context).ConfigureAwait(false);

        _resolvingDatabase(context.Logger, deployment.DatabaseName, resource.Name, null);

        IUpstashRedisManagementClient client = new UpstashRedisManagementClient(_managementHttpClient, deployment.ManagementCredentials);
        UpstashRedisRemoteIdentityDeploymentStateStore identityStore = new(
            context.Services.GetRequiredService<IDeploymentStateManager>());
        UpstashRedisRemoteIdentityState? cachedIdentity =
            await identityStore.LoadAsync(resource.Name, context.CancellationToken).ConfigureAwait(false);

        UpstashRedisCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            identityState => identityStore.SaveAsync(resource.Name, identityState, context.CancellationToken),
            context.CancellationToken)
            .ConfigureAwait(false);

        resource.ApplyUpstashRedisConnectionOutput(result.Database);

        if (result.Created)
        {
            _createdDatabase(context.Logger, deployment.DatabaseName, resource.Name, null);
        }
        else
        {
            _usingExistingDatabase(context.Logger, deployment.DatabaseName, resource.Name, null);
        }
    }

    internal static async Task<UpstashRedisDatabaseDetails?> ExecuteAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        CancellationToken cancellationToken)
    {
        UpstashRedisCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity: null,
            saveIdentityStateAsync: null,
            cancellationToken).ConfigureAwait(false);

        return result.Database;
    }

    internal static async Task<UpstashRedisDatabaseDetails?> ExecuteAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        UpstashRedisRemoteIdentityState? cachedIdentity,
        Func<UpstashRedisRemoteIdentityState, Task>? saveIdentityStateAsync,
        CancellationToken cancellationToken)
    {
        UpstashRedisCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            saveIdentityStateAsync,
            cancellationToken).ConfigureAwait(false);

        return result.Database;
    }

    private static async Task<UpstashRedisCreateFlowResult> ExecuteCoreAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        UpstashRedisRemoteIdentityState? cachedIdentity,
        Func<UpstashRedisRemoteIdentityState, Task>? saveIdentityStateAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(client);

        UpstashRedisRemoteIdentityResolution remoteIdentity =
            await new UpstashRedisRemoteIdentityResolver(client)
                .ResolveAsync(deployment.DatabaseName, cachedIdentity, cancellationToken)
                .ConfigureAwait(false);

        UpstashRedisOwnershipResolutionRequest ownershipRequest = new(
            deployment.DatabaseName,
            deployment.OwnershipMode,
            deployment.Options,
            remoteIdentity.ResolvedFromCachedIdentity,
            remoteIdentity.Database);
        UpstashRedisOwnershipResolutionResult ownership = UpstashRedisOwnershipResolver.Resolve(
            ownershipRequest,
            remoteIdentity.Database);

        UpstashRedisCreateFlowResult createResult =
            await new UpstashRedisCreateFlow(client)
                .ExecuteAsync(deployment, ownership, cancellationToken)
                .ConfigureAwait(false);

        UpstashRedisDatabaseDetails reconciledDatabase = await new UpstashRedisReconciler(client)
            .ReconcileAsync(createResult.Database, deployment.Options, cancellationToken)
            .ConfigureAwait(false);

        UpstashRedisCreateFlowResult result = new(reconciledDatabase, createResult.Created);

        if (saveIdentityStateAsync is not null)
        {
            await saveIdentityStateAsync(result.RemoteIdentity).ConfigureAwait(false);
        }

        return result;
    }
}
