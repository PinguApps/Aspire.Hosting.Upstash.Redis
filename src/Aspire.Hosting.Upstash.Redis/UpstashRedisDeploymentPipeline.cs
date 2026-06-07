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
        UpstashRedisRemoteIdentityResolution remoteIdentity =
            await new UpstashRedisRemoteIdentityResolver(client)
                .ResolveAsync(deployment.DatabaseName, cachedIdentity, context.CancellationToken)
                .ConfigureAwait(false);
        UpstashRedisOwnershipResolutionRequest ownershipRequest = new(
            deployment.DatabaseName,
            deployment.OwnershipMode,
            deployment.Options,
            remoteIdentity.ResolvedFromCachedIdentity);
        UpstashRedisOwnershipResolutionResult ownership = UpstashRedisOwnershipResolver.Resolve(
            ownershipRequest,
            remoteIdentity.Database);

        UpstashRedisCreateFlow createFlow = new(client);
        UpstashRedisCreateFlowResult createResult =
            await createFlow.ExecuteAsync(deployment, ownership, context.CancellationToken).ConfigureAwait(false);

        await identityStore.SaveAsync(resource.Name, createResult.RemoteIdentity, context.CancellationToken)
            .ConfigureAwait(false);

        if (createResult.Created)
        {
            _createdDatabase(context.Logger, deployment.DatabaseName, resource.Name, null);
        }
        else
        {
            _usingExistingDatabase(context.Logger, deployment.DatabaseName, resource.Name, null);
        }
    }
}
