using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal sealed class UpstashRedisRemoteIdentityResolver
{
    private readonly IUpstashRedisManagementClient _client;

    public UpstashRedisRemoteIdentityResolver(IUpstashRedisManagementClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client;
    }

    public async Task<UpstashRedisRemoteIdentityResolution> ResolveAsync(
        string configuredDatabaseName,
        UpstashRedisRemoteIdentityState? cachedIdentity,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredDatabaseName);

        if (cachedIdentity is null)
        {
            return await ResolveByConfiguredNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);
        }

        if (cachedIdentity.DatabaseName != configuredDatabaseName)
        {
            // The explicit name is the v1 identity. A changed configured name selects a different
            // remote database; this package never calls the provider rename endpoint.
            return await ResolveByConfiguredNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            UpstashRedisDatabaseDetails cachedDatabase =
                await _client.GetDatabaseAsync(cachedIdentity.ProviderDatabaseId, cancellationToken).ConfigureAwait(false);

            if (cachedDatabase.DatabaseId != cachedIdentity.ProviderDatabaseId)
            {
                throw CreateMismatchedCachedDetailException(
                    configuredDatabaseName,
                    cachedIdentity.ProviderDatabaseId,
                    cachedDatabase.DatabaseId);
            }

            UpstashRedisRemoteIdentityResolution resolution = UpstashRedisRemoteIdentityResolution.FoundDatabase(
                cachedDatabase,
                resolvedFromCachedIdentity: true);

            if (cachedDatabase.DatabaseName != configuredDatabaseName)
            {
                resolution = await ResolveDriftedCachedIdentityAsync(
                    configuredDatabaseName,
                    cachedIdentity,
                    cachedDatabase.DatabaseName,
                    cancellationToken).ConfigureAwait(false);
            }

            await VerifyConfiguredNameResolvesToCachedIdentityAsync(
                configuredDatabaseName,
                cachedIdentity.ProviderDatabaseId,
                cancellationToken).ConfigureAwait(false);

            return resolution;
        }
        catch (UpstashRedisProviderException exception) when (exception.FailureKind == UpstashRedisProviderFailureKind.NotFound)
        {
            return await ResolveMissingCachedIdentityAsync(configuredDatabaseName, cachedIdentity, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<UpstashRedisRemoteIdentityResolution> ResolveByConfiguredNameAsync(
        string configuredDatabaseName,
        CancellationToken cancellationToken)
    {
        UpstashRedisDatabaseDetails? database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);

        return database is null
            ? UpstashRedisRemoteIdentityResolution.NotFound()
            : UpstashRedisRemoteIdentityResolution.FoundDatabase(database);
    }

    private async Task<UpstashRedisRemoteIdentityResolution> ResolveMissingCachedIdentityAsync(
        string configuredDatabaseName,
        UpstashRedisRemoteIdentityState cachedIdentity,
        CancellationToken cancellationToken)
    {
        UpstashRedisDatabaseDetails? database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);

        if (database is null)
        {
            return UpstashRedisRemoteIdentityResolution.NotFound();
        }

        if (database.DatabaseId != cachedIdentity.ProviderDatabaseId)
        {
            throw CreateUnsafeIdentityException(
                configuredDatabaseName,
                cachedIdentity.ProviderDatabaseId,
                database.DatabaseId);
        }

        return UpstashRedisRemoteIdentityResolution.FoundDatabase(
            database,
            resolvedFromCachedIdentity: true);
    }

    private async Task<UpstashRedisRemoteIdentityResolution> ResolveDriftedCachedIdentityAsync(
        string configuredDatabaseName,
        UpstashRedisRemoteIdentityState cachedIdentity,
        string currentCachedDatabaseName,
        CancellationToken cancellationToken)
    {
        UpstashRedisDatabaseDetails? database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false);

        if (database is not null && database.DatabaseId != cachedIdentity.ProviderDatabaseId)
        {
            throw CreateUnsafeIdentityException(
                configuredDatabaseName,
                cachedIdentity.ProviderDatabaseId,
                database.DatabaseId);
        }

        throw new UpstashRedisProviderException(
            UpstashRedisProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Cached Upstash Redis database '{cachedIdentity.ProviderDatabaseId}' is now named '{currentCachedDatabaseName}', not configured name '{configuredDatabaseName}'. Refusing to reconcile a drifted remote identity.");
    }

    private async Task VerifyConfiguredNameResolvesToCachedIdentityAsync(
        string configuredDatabaseName,
        string cachedProviderDatabaseId,
        CancellationToken cancellationToken)
    {
        UpstashRedisDatabaseDetails database =
            await _client.FindDatabaseByNameAsync(configuredDatabaseName, cancellationToken).ConfigureAwait(false)
            ?? throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Cached Upstash Redis database '{cachedProviderDatabaseId}' still reports configured name '{configuredDatabaseName}', but the configured name lookup returned no database. Refusing to reconcile an unverifiable cached remote identity.");

        if (database.DatabaseId != cachedProviderDatabaseId)
        {
            throw CreateUnsafeIdentityException(
                configuredDatabaseName,
                cachedProviderDatabaseId,
                database.DatabaseId);
        }
    }

    private static UpstashRedisProviderException CreateUnsafeIdentityException(
        string configuredDatabaseName,
        string cachedProviderDatabaseId,
        string resolvedProviderDatabaseId)
    {
        return new UpstashRedisProviderException(
            UpstashRedisProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Configured Upstash Redis database name '{configuredDatabaseName}' resolves to provider id '{resolvedProviderDatabaseId}', but cached identity expected provider id '{cachedProviderDatabaseId}'. Refusing to adopt a different database for the same configured name.");
    }

    private static UpstashRedisProviderException CreateMismatchedCachedDetailException(
        string configuredDatabaseName,
        string cachedProviderDatabaseId,
        string resolvedProviderDatabaseId)
    {
        return new UpstashRedisProviderException(
            UpstashRedisProviderFailureKind.ProviderContract,
            statusCode: null,
            $"Cached Upstash Redis database '{cachedProviderDatabaseId}' detail response returned provider id '{resolvedProviderDatabaseId}' for configured name '{configuredDatabaseName}'. Refusing to reconcile a mismatched cached remote identity.");
    }
}
