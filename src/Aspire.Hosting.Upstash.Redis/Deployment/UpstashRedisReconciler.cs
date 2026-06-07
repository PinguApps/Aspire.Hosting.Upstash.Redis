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

        if (PlanMatches(desiredPlan, current))
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

            if (!PlanMatches(desiredPlan, current))
            {
                throw CreateVerificationException(current, "plan", desiredPlan, FormatPlan(current));
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

    private static string FormatPlan(UpstashRedisDatabaseDetails current)
    {
        return current.DbDiskThreshold is null
            ? current.Type ?? "<unset>"
            : $"{current.Type ?? "<unset>"} ({current.DbDiskThreshold.Value} bytes)";
    }

    private static bool PlanMatches(string desiredPlan, UpstashRedisDatabaseDetails current)
    {
        long? desiredFixedPlanBytes = GetFixedPlanBytes(desiredPlan);

        if (desiredFixedPlanBytes is not null)
        {
            return StringComparer.Ordinal.Equals(current.Type, "pro")
                && current.DbDiskThreshold == desiredFixedPlanBytes;
        }

        return StringComparer.Ordinal.Equals(desiredPlan, NormalizeProviderPlan(current.Type));
    }

    private static string? NormalizeProviderPlan(string? plan)
    {
        return StringComparer.Ordinal.Equals(plan, "paid")
            ? "payg"
            : plan;
    }

    private static long? GetFixedPlanBytes(string desiredPlan)
    {
        const long mebibyte = 1024L * 1024L;
        const long gibibyte = 1024L * mebibyte;

        switch (desiredPlan)
        {
            case "fixed_250mb":
                return 250L * mebibyte;
            case "fixed_1gb":
                return 1L * gibibyte;
            case "fixed_5gb":
                return 5L * gibibyte;
            case "fixed_10gb":
                return 10L * gibibyte;
            case "fixed_50gb":
                return 50L * gibibyte;
            case "fixed_100gb":
                return 100L * gibibyte;
            case "fixed_500gb":
                return 500L * gibibyte;
            default:
                return null;
        }
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
