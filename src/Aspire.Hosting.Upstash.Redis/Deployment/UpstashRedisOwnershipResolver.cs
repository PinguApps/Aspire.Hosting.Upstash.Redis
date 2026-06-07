using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal static class UpstashRedisOwnershipResolver
{
    public static async Task<UpstashRedisOwnershipResolutionResult> ResolveAsync(
        UpstashRedisOwnershipResolutionRequest request,
        IUpstashRedisManagementClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(client);

        UpstashRedisDatabaseDetails? existingDatabase = await client
            .FindDatabaseByNameAsync(request.DatabaseName, cancellationToken)
            .ConfigureAwait(false);

        return request.OwnershipMode switch
        {
            UpstashRedisOwnershipMode.CreateOnly => ResolveCreateOnly(request.DatabaseName, existingDatabase),
            UpstashRedisOwnershipMode.ExistingOnly => ResolveExistingOnly(request, existingDatabase),
            UpstashRedisOwnershipMode.CreateOrAdopt => ResolveCreateOrAdopt(request, existingDatabase),
            _ => throw new ArgumentOutOfRangeException(
                nameof(request),
                request.OwnershipMode,
                "The Upstash Redis ownership mode is not supported.")
        };
    }

    private static UpstashRedisOwnershipResolutionResult ResolveCreateOnly(
        string databaseName,
        UpstashRedisDatabaseDetails? existingDatabase)
    {
        return existingDatabase is not null
            ? throw new UpstashRedisOwnershipResolutionException(
                UpstashRedisOwnershipResolutionFailureReason.CreateOnlyDatabaseAlreadyExists,
                $"Upstash Redis database '{databaseName}' already exists, but ownership mode is create-only. Choose a different database name, delete the existing database outside Aspire, or use create-or-adopt/existing-only if this deployment should manage it.")
            : UpstashRedisOwnershipResolutionResult.Create();
    }

    private static UpstashRedisOwnershipResolutionResult ResolveExistingOnly(
        UpstashRedisOwnershipResolutionRequest request,
        UpstashRedisDatabaseDetails? existingDatabase)
    {
        if (existingDatabase is null)
        {
            throw new UpstashRedisOwnershipResolutionException(
                UpstashRedisOwnershipResolutionFailureReason.ExistingOnlyDatabaseMissing,
                $"Upstash Redis database '{request.DatabaseName}' does not exist, but ownership mode is existing-only. Create the database outside Aspire first or use create-or-adopt/create-only if this deployment may create it.");
        }

        ValidateExistingDatabaseCompatibility(request, existingDatabase);

        return UpstashRedisOwnershipResolutionResult.Adopt(existingDatabase);
    }

    private static UpstashRedisOwnershipResolutionResult ResolveCreateOrAdopt(
        UpstashRedisOwnershipResolutionRequest request,
        UpstashRedisDatabaseDetails? existingDatabase)
    {
        if (existingDatabase is null)
        {
            return UpstashRedisOwnershipResolutionResult.Create();
        }

        ValidateExistingDatabaseCompatibility(request, existingDatabase);

        return UpstashRedisOwnershipResolutionResult.Adopt(existingDatabase);
    }

    private static void ValidateExistingDatabaseCompatibility(
        UpstashRedisOwnershipResolutionRequest request,
        UpstashRedisDatabaseDetails existingDatabase)
    {
        if (request.Options.PrimaryRegion?.LiteralValue is string requestedPrimaryRegion
            && !StringComparer.Ordinal.Equals(requestedPrimaryRegion, existingDatabase.PrimaryRegion))
        {
            throw CreateIncompatibleDatabaseException(
                request.DatabaseName,
                "primary region",
                requestedPrimaryRegion,
                existingDatabase.PrimaryRegion ?? "<unset>");
        }

        if (request.Options.Tls?.LiteralValue is true && !existingDatabase.Tls)
        {
            throw CreateIncompatibleDatabaseException(
                request.DatabaseName,
                "TLS",
                "enabled",
                "disabled");
        }
    }

    private static UpstashRedisOwnershipResolutionException CreateIncompatibleDatabaseException(
        string databaseName,
        string settingName,
        string requestedValue,
        string actualValue)
    {
        return new UpstashRedisOwnershipResolutionException(
            UpstashRedisOwnershipResolutionFailureReason.ExistingDatabaseIncompatible,
            $"Upstash Redis database '{databaseName}' already exists but is incompatible with the requested explicit {settingName}. Requested '{requestedValue}', found '{actualValue}'. The package will not replace or mutate this setting automatically in v1.");
    }
}
