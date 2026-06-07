using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisReconciler
{
    private readonly IUpstashRedisManagementClient _client;

    public UpstashRedisReconciler(IUpstashRedisManagementClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client;
    }

    public async Task<UpstashRedisDatabaseDetails> ReconcileAsync(
        UpstashRedisDatabaseDetails database,
        UpstashRedisProviderDeploymentOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(options);

        UpstashRedisDatabaseDetails current = database;

        current = await ReconcileReadRegionsAsync(current, options, cancellationToken).ConfigureAwait(false);
        current = await ReconcilePlanAsync(current, options, cancellationToken).ConfigureAwait(false);
        current = await ReconcileBudgetAsync(current, options, cancellationToken).ConfigureAwait(false);
        current = await ReconcileEvictionAsync(current, options, cancellationToken).ConfigureAwait(false);

        VerifyFinalState(current, options);

        return current;
    }

    private async Task<UpstashRedisDatabaseDetails> ReconcileReadRegionsAsync(
        UpstashRedisDatabaseDetails current,
        UpstashRedisProviderDeploymentOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions))
            || options.ReadRegions is null)
        {
            return current;
        }

        string[] desiredReadRegions = [.. options.ReadRegions.Select(GetRequiredStringLiteral)];

        if (ReadRegionsMatch(desiredReadRegions, current.ReadRegions))
        {
            return current;
        }

        try
        {
            await _client.UpdateReadRegionsAsync(
                current.DatabaseId,
                new UpstashRedisUpdateRegionsRequest { ReadRegions = desiredReadRegions },
                cancellationToken).ConfigureAwait(false);

            return await _client
                .WaitUntilReadyAsync(current.DatabaseId, UpstashRedisReadinessPollingOptions.Default, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (UpstashRedisProviderException exception)
        {
            throw CreateMutationException(
                current,
                "read regions",
                string.Join(", ", desiredReadRegions),
                exception);
        }
    }

    private async Task<UpstashRedisDatabaseDetails> ReconcilePlanAsync(
        UpstashRedisDatabaseDetails current,
        UpstashRedisProviderDeploymentOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Plan))
            || options.Plan is null)
        {
            return current;
        }

        string desiredPlan = GetRequiredStringLiteral(options.Plan);

        if (StringComparer.Ordinal.Equals(desiredPlan, current.Type))
        {
            return current;
        }

        try
        {
            await _client.ChangePlanAsync(
                current.DatabaseId,
                new UpstashRedisChangePlanRequest { PlanName = desiredPlan },
                cancellationToken).ConfigureAwait(false);

            return await _client
                .WaitUntilReadyAsync(current.DatabaseId, UpstashRedisReadinessPollingOptions.Default, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (UpstashRedisProviderException exception)
        {
            throw CreateMutationException(current, "plan", desiredPlan, exception);
        }
    }

    private async Task<UpstashRedisDatabaseDetails> ReconcileBudgetAsync(
        UpstashRedisDatabaseDetails current,
        UpstashRedisProviderDeploymentOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Budget))
            || options.Budget is null)
        {
            return current;
        }

        int desiredBudget = GetRequiredIntLiteral(options.Budget);

        if (current.Budget == desiredBudget)
        {
            return current;
        }

        try
        {
            await _client.UpdateBudgetAsync(
                current.DatabaseId,
                new UpstashRedisUpdateBudgetRequest { Budget = desiredBudget },
                cancellationToken).ConfigureAwait(false);

            return await _client
                .WaitUntilReadyAsync(current.DatabaseId, UpstashRedisReadinessPollingOptions.Default, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (UpstashRedisProviderException exception)
        {
            throw CreateMutationException(current, "budget", desiredBudget.ToString(System.Globalization.CultureInfo.InvariantCulture), exception);
        }
    }

    private async Task<UpstashRedisDatabaseDetails> ReconcileEvictionAsync(
        UpstashRedisDatabaseDetails current,
        UpstashRedisProviderDeploymentOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Eviction))
            || options.Eviction is null)
        {
            return current;
        }

        bool desiredEviction = GetRequiredBoolLiteral(options.Eviction);

        if (current.Eviction == desiredEviction)
        {
            return current;
        }

        try
        {
            await _client.SetEvictionAsync(current.DatabaseId, desiredEviction, cancellationToken).ConfigureAwait(false);

            return await _client
                .WaitUntilReadyAsync(current.DatabaseId, UpstashRedisReadinessPollingOptions.Default, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (UpstashRedisProviderException exception)
        {
            throw CreateMutationException(current, "eviction", desiredEviction ? "enabled" : "disabled", exception);
        }
    }

    private static void VerifyFinalState(
        UpstashRedisDatabaseDetails current,
        UpstashRedisProviderDeploymentOptions options)
    {
        if (options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions))
            && options.ReadRegions is not null)
        {
            string[] desiredReadRegions = [.. options.ReadRegions.Select(GetRequiredStringLiteral)];

            if (!ReadRegionsMatch(desiredReadRegions, current.ReadRegions))
            {
                throw CreateVerificationException(
                    current,
                    "read regions",
                    string.Join(", ", desiredReadRegions),
                    string.Join(", ", current.ReadRegions ?? []));
            }
        }

        if (options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Plan))
            && options.Plan is not null)
        {
            string desiredPlan = GetRequiredStringLiteral(options.Plan);

            if (!StringComparer.Ordinal.Equals(desiredPlan, current.Type))
            {
                throw CreateVerificationException(current, "plan", desiredPlan, current.Type ?? "<unset>");
            }
        }

        if (options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Budget))
            && options.Budget is not null)
        {
            int desiredBudget = GetRequiredIntLiteral(options.Budget);

            if (current.Budget != desiredBudget)
            {
                string actualBudget = current.Budget?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<unset>";

                throw CreateVerificationException(
                    current,
                    "budget",
                    desiredBudget.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    actualBudget);
            }
        }

        if (options.ExplicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Eviction))
            && options.Eviction is not null)
        {
            bool desiredEviction = GetRequiredBoolLiteral(options.Eviction);

            if (current.Eviction != desiredEviction)
            {
                string actualEviction = FormatEviction(current.Eviction);

                throw CreateVerificationException(
                    current,
                    "eviction",
                    FormatEviction(desiredEviction),
                    actualEviction);
            }
        }
    }

    private static string GetRequiredStringLiteral(UpstashRedisProviderValue value)
    {
        return value.LiteralValue as string
            ?? throw new InvalidOperationException("Resolved Upstash Redis deployment options must contain string provider values before reconciliation.");
    }

    private static int GetRequiredIntLiteral(UpstashRedisProviderValue value)
    {
        return value.LiteralValue is int literalValue
            ? literalValue
            : throw new InvalidOperationException("Resolved Upstash Redis deployment options must contain integer provider values before reconciliation.");
    }

    private static bool GetRequiredBoolLiteral(UpstashRedisProviderValue value)
    {
        return value.LiteralValue is bool literalValue
            ? literalValue
            : throw new InvalidOperationException("Resolved Upstash Redis deployment options must contain boolean provider values before reconciliation.");
    }

    private static string FormatEviction(bool? eviction)
    {
        if (eviction is null)
        {
            return "<unset>";
        }

        return eviction.Value ? "enabled" : "disabled";
    }

    private static bool ReadRegionsMatch(IReadOnlyList<string> desiredReadRegions, IReadOnlyList<string>? actualReadRegions)
    {
        return desiredReadRegions.Count == (actualReadRegions?.Count ?? 0)
            && desiredReadRegions.Order(StringComparer.Ordinal).SequenceEqual((actualReadRegions ?? []).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static UpstashRedisReconciliationException CreateMutationException(
        UpstashRedisDatabaseDetails current,
        string settingName,
        string desiredValue,
        UpstashRedisProviderException exception)
    {
        return new UpstashRedisReconciliationException(
            settingName,
            exception.FailureKind,
            $"Failed to reconcile Upstash Redis database '{current.DatabaseName}' setting '{settingName}' to '{desiredValue}'. {exception.Message}",
            exception);
    }

    private static UpstashRedisReconciliationException CreateVerificationException(
        UpstashRedisDatabaseDetails current,
        string settingName,
        string desiredValue,
        string actualValue)
    {
        return new UpstashRedisReconciliationException(
            settingName,
            UpstashRedisProviderFailureKind.ProviderContract,
            $"Upstash Redis database '{current.DatabaseName}' did not converge after reconciling setting '{settingName}'. Requested '{desiredValue}', found '{actualValue}'.");
    }
}
