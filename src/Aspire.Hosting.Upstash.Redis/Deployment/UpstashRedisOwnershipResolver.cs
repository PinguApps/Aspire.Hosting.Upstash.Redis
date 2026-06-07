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

        UpstashRedisDatabaseDetails? existingDatabase = request.ExistingDatabase;

        existingDatabase ??= await client
            .FindDatabaseByNameAsync(request.DatabaseName, cancellationToken)
            .ConfigureAwait(false);

        switch (request.OwnershipMode)
        {
            case UpstashRedisOwnershipMode.CreateOnly:
                return ResolveCreateOnly(request.DatabaseName, existingDatabase);
            case UpstashRedisOwnershipMode.ExistingOnly:
                return ResolveExistingOnly(request, existingDatabase);
            case UpstashRedisOwnershipMode.CreateOrAdopt:
                return ResolveCreateOrAdopt(request, existingDatabase);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.OwnershipMode,
                    "The Upstash Redis ownership mode is not supported.");
        }
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
        UpstashRedisImmutableDrift? drift = UpstashRedisImmutableDriftDetector.Detect(
            request.DatabaseName,
            request.Options,
            existingDatabase);

        if (drift is not null)
        {
            throw new UpstashRedisOwnershipResolutionException(
                UpstashRedisOwnershipResolutionFailureReason.ExistingDatabaseIncompatible,
                drift.Message);
        }
    }
}
