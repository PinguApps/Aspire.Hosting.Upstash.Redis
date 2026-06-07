using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisCreateFlow
{
    private readonly IUpstashRedisManagementClient _client;
    private readonly UpstashRedisReadinessPollingOptions _readinessPollingOptions;

    public UpstashRedisCreateFlow(
        IUpstashRedisManagementClient client,
        UpstashRedisReadinessPollingOptions? readinessPollingOptions = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client;
        _readinessPollingOptions = readinessPollingOptions ?? UpstashRedisReadinessPollingOptions.Default;
    }

    public async Task<UpstashRedisCreateFlowResult> ExecuteAsync(
        UpstashRedisResolvedDeployment deployment,
        UpstashRedisOwnershipResolutionResult ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(ownership);

        if (ownership.Action != UpstashRedisOwnershipResolutionAction.Create)
        {
            UpstashRedisDatabaseDetails adoptedDatabase = ownership.Database
                ?? throw new InvalidOperationException("Upstash Redis ownership resolution selected adopt without a database.");

            ValidateConnectionDetails(deployment.DatabaseName, adoptedDatabase);

            return new UpstashRedisCreateFlowResult(adoptedDatabase, created: false);
        }

        UpstashRedisCreateDatabaseRequest request = BuildCreateRequest(deployment);
        UpstashRedisDatabaseDetails createdDatabase;

        try
        {
            createdDatabase = await _client.CreateDatabaseAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (UpstashRedisProviderException exception)
        {
            throw new InvalidOperationException(
                $"Failed to create Upstash Redis database '{deployment.DatabaseName}': {exception.Message}",
                exception);
        }

        string databaseId = string.IsNullOrWhiteSpace(createdDatabase.DatabaseId)
            ? throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis create response for database '{deployment.DatabaseName}' did not include a provider database id.")
            : createdDatabase.DatabaseId;

        UpstashRedisDatabaseDetails readyDatabase = await _client
            .WaitUntilReadyAsync(databaseId, _readinessPollingOptions, cancellationToken)
            .ConfigureAwait(false);

        ValidateCreatedDatabase(deployment.DatabaseName, databaseId, readyDatabase);
        ValidateConnectionDetails(deployment.DatabaseName, readyDatabase);

        return new UpstashRedisCreateFlowResult(readyDatabase, created: true);
    }

    private static UpstashRedisCreateDatabaseRequest BuildCreateRequest(UpstashRedisResolvedDeployment deployment)
    {
        UpstashRedisProviderDeploymentOptions options = deployment.Options;
        bool tls = GetOptionalBoolean(options.Tls, nameof(UpstashRedisDeploymentOptions.Tls)) ?? true;

        if (!tls)
        {
            throw new InvalidOperationException("Upstash Redis requires TLS for v1 deployments. Set TLS to true or leave it unset.");
        }

        return new UpstashRedisCreateDatabaseRequest
        {
            DatabaseName = deployment.DatabaseName,
            Platform = GetRequiredString(options.Platform, nameof(UpstashRedisDeploymentOptions.Platform), "platform"),
            PrimaryRegion = GetRequiredString(options.PrimaryRegion, nameof(UpstashRedisDeploymentOptions.PrimaryRegion), "primary region"),
            ReadRegions = options.ReadRegions is null ? null : [.. options.ReadRegions.Select(GetReadRegion)],
            Plan = GetOptionalString(options.Plan, nameof(UpstashRedisDeploymentOptions.Plan)),
            Budget = GetOptionalInt32(options.Budget, nameof(UpstashRedisDeploymentOptions.Budget)),
            Eviction = GetOptionalBoolean(options.Eviction, nameof(UpstashRedisDeploymentOptions.Eviction)),
            Tls = true,
        };
    }

    private static void ValidateCreatedDatabase(
        string configuredDatabaseName,
        string createdDatabaseId,
        UpstashRedisDatabaseDetails readyDatabase)
    {
        if (readyDatabase.DatabaseId != createdDatabaseId)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis readiness lookup for created database '{createdDatabaseId}' returned provider id '{readyDatabase.DatabaseId}'.");
        }

        if (readyDatabase.DatabaseName != configuredDatabaseName)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis readiness lookup for created database '{createdDatabaseId}' returned database name '{readyDatabase.DatabaseName}', not configured name '{configuredDatabaseName}'.");
        }
    }

    private static void ValidateConnectionDetails(
        string configuredDatabaseName,
        UpstashRedisDatabaseDetails database)
    {
        string databaseId = database.DatabaseId;

        if (database.DatabaseName != configuredDatabaseName)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' with name '{database.DatabaseName}', not configured name '{configuredDatabaseName}'.");
        }

        if (string.IsNullOrWhiteSpace(database.Password))
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' without credentials.");
        }

        if (string.IsNullOrWhiteSpace(database.Endpoint))
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' without an endpoint.");
        }

        if (database.Port <= 0)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' without a valid port.");
        }

        if (!database.Tls)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned database '{databaseId}' with TLS disabled.");
        }
    }

    private static string GetRequiredString(
        UpstashRedisProviderValue? value,
        string optionName,
        string settingName)
    {
        string? literalValue = GetOptionalString(value, optionName);

        return string.IsNullOrWhiteSpace(literalValue)
            ? throw new InvalidOperationException($"Upstash Redis create requires an explicit {settingName}. Configure {optionName} before deploying a new database.")
            : literalValue;
    }

    private static string? GetOptionalString(UpstashRedisProviderValue? value, string optionName)
    {
        if (value is null)
        {
            return null;
        }

        return value.LiteralValue as string
            ?? throw new InvalidOperationException($"Upstash Redis option {optionName} was not resolved to a provider string value.");
    }

    private static int? GetOptionalInt32(UpstashRedisProviderValue? value, string optionName)
    {
        if (value is null)
        {
            return null;
        }

        return value.LiteralValue is int intValue
            ? intValue
            : throw new InvalidOperationException($"Upstash Redis option {optionName} was not resolved to a provider integer value.");
    }

    private static bool? GetOptionalBoolean(UpstashRedisProviderValue? value, string optionName)
    {
        if (value is null)
        {
            return null;
        }

        return value.LiteralValue is bool boolValue
            ? boolValue
            : throw new InvalidOperationException($"Upstash Redis option {optionName} was not resolved to a provider boolean value.");
    }

    private static string GetReadRegion(UpstashRedisProviderValue value)
    {
        return GetRequiredString(value, nameof(UpstashRedisDeploymentOptions.ReadRegions), "read region");
    }
}
