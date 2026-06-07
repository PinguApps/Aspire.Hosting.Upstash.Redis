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

    private static readonly Action<ILogger, string, string?, string?, string?, Exception?> _deploymentProgress =
        LoggerMessage.Define<string, string?, string?, string?>(
            LogLevel.Information,
            new EventId(1, "UpstashRedisDeploymentProgress"),
            "{Message} Resource='{ResourceName}' Database='{DatabaseName}' ProviderDatabaseId='{ProviderDatabaseId}'.");

    public static async Task ExecuteAsync(RedisResource resource, PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(context);

        LoggerUpstashRedisDeploymentProgressReporter progressReporter = new(context.Logger, resource.Name);
        progressReporter.Report(UpstashRedisDeploymentDiagnostics.CreateProgress(
            UpstashRedisDeploymentPhase.ResolvingConfiguration,
            $"Resolving Upstash Redis deployment configuration for Redis resource '{resource.Name}'.",
            resource.Name,
            databaseName: null,
            providerDatabaseId: null));

        UpstashRedisDeploymentState state = resource.GetUpstashRedisDeploymentState()
            ?? throw new InvalidOperationException($"Redis resource '{resource.Name}' is missing Upstash deployment state.");

        UpstashRedisResolvedDeployment deployment =
            await UpstashRedisDeployTimeResolver.ResolveAsync(state, resource, context).ConfigureAwait(false);

        IUpstashRedisManagementClient client = new UpstashRedisManagementClient(_managementHttpClient, deployment.ManagementCredentials);
        UpstashRedisRemoteIdentityDeploymentStateStore identityStore = new(
            context.Services.GetRequiredService<IDeploymentStateManager>());
        UpstashRedisRemoteIdentityState? cachedIdentity =
            await identityStore.LoadAsync(resource.Name, context.CancellationToken).ConfigureAwait(false);

        UpstashRedisCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            progressReporter,
            resource.Name,
            context.CancellationToken)
            .ConfigureAwait(false);

        resource.ApplyUpstashRedisConnectionOutput(result.Database);

        await identityStore.SaveAsync(resource.Name, result.RemoteIdentity, context.CancellationToken).ConfigureAwait(false);
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
            progressReporter: null,
            resourceName: null,
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
            progressReporter: null,
            resourceName: null,
            cancellationToken).ConfigureAwait(false);

        if (saveIdentityStateAsync is not null)
        {
            await saveIdentityStateAsync(result.RemoteIdentity).ConfigureAwait(false);
        }

        return result.Database;
    }

    internal static async Task<UpstashRedisDatabaseDetails?> ExecuteAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        UpstashRedisRemoteIdentityState? cachedIdentity,
        Func<UpstashRedisRemoteIdentityState, Task>? saveIdentityStateAsync,
        IUpstashRedisDeploymentProgressReporter? progressReporter,
        string? resourceName,
        CancellationToken cancellationToken)
    {
        UpstashRedisCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            progressReporter,
            resourceName,
            cancellationToken).ConfigureAwait(false);

        if (saveIdentityStateAsync is not null)
        {
            await saveIdentityStateAsync(result.RemoteIdentity).ConfigureAwait(false);
        }

        return result.Database;
    }

    private static async Task<UpstashRedisCreateFlowResult> ExecuteCoreAsync(
        UpstashRedisResolvedDeployment deployment,
        IUpstashRedisManagementClient client,
        UpstashRedisRemoteIdentityState? cachedIdentity,
        IUpstashRedisDeploymentProgressReporter? progressReporter,
        string? resourceName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(client);

        Report(
            progressReporter,
            UpstashRedisDeploymentPhase.ResolvingConfiguration,
            $"Resolved Upstash Redis deployment configuration for database '{deployment.DatabaseName}'.",
            resourceName,
            deployment.DatabaseName,
            providerDatabaseId: null,
            deployment,
            database: null);

        Report(
            progressReporter,
            UpstashRedisDeploymentPhase.LocatingDatabase,
            $"Locating Upstash Redis database '{deployment.DatabaseName}' by configured name.",
            resourceName,
            deployment.DatabaseName,
            providerDatabaseId: null,
            deployment,
            database: null);

        UpstashRedisRemoteIdentityResolution remoteIdentity =
            await new UpstashRedisRemoteIdentityResolver(client)
                .ResolveAsync(deployment.DatabaseName, cachedIdentity, cancellationToken)
                .ConfigureAwait(false);

        string? locatedProviderDatabaseId = remoteIdentity.Database?.DatabaseId;
        string locatedMessage = remoteIdentity.Database is null
            ? $"No Upstash Redis database named '{deployment.DatabaseName}' was found."
            : $"Located Upstash Redis database '{deployment.DatabaseName}' with provider id '{UpstashRedisDeploymentDiagnostics.FormatProviderDatabaseId(locatedProviderDatabaseId)}'.";

        Report(
            progressReporter,
            UpstashRedisDeploymentPhase.LocatingDatabase,
            locatedMessage,
            resourceName,
            deployment.DatabaseName,
            locatedProviderDatabaseId,
            deployment,
            remoteIdentity.Database);

        Report(
            progressReporter,
            UpstashRedisDeploymentPhase.ValidatingImmutableDrift,
            $"Validating immutable Upstash Redis settings for database '{deployment.DatabaseName}'.",
            resourceName,
            deployment.DatabaseName,
            locatedProviderDatabaseId,
            deployment,
            remoteIdentity.Database);

        UpstashRedisOwnershipResolutionRequest ownershipRequest = new(
            deployment.DatabaseName,
            deployment.OwnershipMode,
            deployment.Options,
            remoteIdentity.ResolvedFromCachedIdentity,
            remoteIdentity.Database);
        UpstashRedisOwnershipResolutionResult ownership = UpstashRedisOwnershipResolver.Resolve(
            ownershipRequest,
            remoteIdentity.Database);

        ReportOwnership(progressReporter, resourceName, deployment, ownership);

        UpstashRedisCreateFlowResult createResult =
            await new UpstashRedisCreateFlow(client)
                .ExecuteAsync(deployment, ownership, cancellationToken)
                .ConfigureAwait(false);

        ReportCreatedOrAdopted(progressReporter, resourceName, deployment, createResult);

        Report(
            progressReporter,
            UpstashRedisDeploymentPhase.ReconcilingMutableSettings,
            $"Reconciling explicit mutable Upstash Redis settings for database '{deployment.DatabaseName}'.",
            resourceName,
            deployment.DatabaseName,
            createResult.Database.DatabaseId,
            deployment,
            createResult.Database);

        UpstashRedisDatabaseDetails reconciledDatabase = await new UpstashRedisReconciler(client)
            .ReconcileAsync(createResult.Database, deployment.Options, cancellationToken)
            .ConfigureAwait(false);

        Report(
            progressReporter,
            UpstashRedisDeploymentPhase.RetrievingOutputs,
            $"Retrieved Redis connection outputs for Upstash Redis database '{deployment.DatabaseName}' with provider id '{UpstashRedisDeploymentDiagnostics.FormatProviderDatabaseId(reconciledDatabase.DatabaseId)}'.",
            resourceName,
            deployment.DatabaseName,
            reconciledDatabase.DatabaseId,
            deployment,
            reconciledDatabase);

        UpstashRedisCreateFlowResult result = new(reconciledDatabase, createResult.Created);

        return result;
    }

    private static void ReportOwnership(
        IUpstashRedisDeploymentProgressReporter? progressReporter,
        string? resourceName,
        UpstashRedisResolvedDeployment deployment,
        UpstashRedisOwnershipResolutionResult ownership)
    {
        if (ownership.Action == UpstashRedisOwnershipResolutionAction.Create)
        {
            Report(
                progressReporter,
                UpstashRedisDeploymentPhase.CreatingDatabase,
                $"Creating Upstash Redis database '{deployment.DatabaseName}'.",
                resourceName,
                deployment.DatabaseName,
                providerDatabaseId: null,
                deployment,
                database: null);
        }
    }

    private static void ReportCreatedOrAdopted(
        IUpstashRedisDeploymentProgressReporter? progressReporter,
        string? resourceName,
        UpstashRedisResolvedDeployment deployment,
        UpstashRedisCreateFlowResult createResult)
    {
        string action = createResult.Created ? "Created" : "Using existing";

        Report(
            progressReporter,
            createResult.Created ? UpstashRedisDeploymentPhase.CreatingDatabase : UpstashRedisDeploymentPhase.LocatingDatabase,
            $"{action} Upstash Redis database '{deployment.DatabaseName}' with provider id '{UpstashRedisDeploymentDiagnostics.FormatProviderDatabaseId(createResult.Database.DatabaseId)}'.",
            resourceName,
            deployment.DatabaseName,
            createResult.Database.DatabaseId,
            deployment,
            createResult.Database);
    }

    private static void Report(
        IUpstashRedisDeploymentProgressReporter? progressReporter,
        UpstashRedisDeploymentPhase phase,
        string message,
        string? resourceName,
        string? databaseName,
        string? providerDatabaseId,
        UpstashRedisResolvedDeployment deployment,
        UpstashRedisDatabaseDetails? database)
    {
        progressReporter?.Report(UpstashRedisDeploymentDiagnostics.CreateProgress(
            phase,
            message,
            resourceName,
            databaseName,
            providerDatabaseId,
            deployment,
            database));
    }

    private sealed class LoggerUpstashRedisDeploymentProgressReporter : IUpstashRedisDeploymentProgressReporter
    {
        private readonly ILogger _logger;
        private readonly string _resourceName;

        public LoggerUpstashRedisDeploymentProgressReporter(ILogger logger, string resourceName)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

            _logger = logger;
            _resourceName = resourceName;
        }

        public void Report(UpstashRedisDeploymentProgress progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            _deploymentProgress(
                _logger,
                progress.Message,
                progress.ResourceName ?? _resourceName,
                progress.DatabaseName,
                progress.ProviderDatabaseId,
                null);
        }
    }
}
