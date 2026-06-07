#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis;

internal static class UpstashRedisDeployTimeResolver
{
    public static Task<UpstashRedisResolvedDeployment> ResolveAsync(
        UpstashRedisDeploymentState state,
        RedisResource resource,
        PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ResolveAsync(
            state,
            resource,
            context.ExecutionContext,
            context.CancellationToken);
    }

    public static async Task<UpstashRedisResolvedDeployment> ResolveAsync(
        UpstashRedisDeploymentState state,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(caller);

        string databaseName = await ResolveRequiredStringAsync(state.DatabaseName, "database name", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string accountEmail = await ResolveRequiredStringAsync(state.AccountEmail, "account email", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string apiKey = await ResolveRequiredStringAsync(state.ApiKey, "API key", caller, executionContext, cancellationToken).ConfigureAwait(false);
        UpstashRedisProviderDeploymentOptions options = await ResolveOptionsAsync(state.Options, caller, executionContext, cancellationToken).ConfigureAwait(false);

        return new UpstashRedisResolvedDeployment(
            databaseName,
            state.OwnershipMode,
            new UpstashRedisManagementCredentials(accountEmail, apiKey),
            options);
    }

    private static async Task<UpstashRedisProviderDeploymentOptions> ResolveOptionsAsync(
        UpstashRedisDeploymentOptions source,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        UpstashRedisDeploymentOptions resolved = new();
        IReadOnlySet<string> explicitSettings = source.ExplicitSettings;

        if (explicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Platform)))
        {
            resolved.Platform = source.Platform is null
                ? null
                : UpstashRedisValue.FromString(await ResolveOptionalStringAsync(source.Platform, "platform", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.PrimaryRegion)))
        {
            resolved.PrimaryRegion = source.PrimaryRegion is null
                ? null
                : UpstashRedisValue.FromString(await ResolveOptionalStringAsync(source.PrimaryRegion, "primary region", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.ReadRegions)))
        {
            if (source.ReadRegions is null)
            {
                resolved.ReadRegions = null;
            }
            else
            {
                List<UpstashRedisValue> readRegions = [];

                foreach (UpstashRedisValue readRegion in source.ReadRegions)
                {
                    string resolvedReadRegion = await ResolveOptionalStringAsync(readRegion, "read region", caller, executionContext, cancellationToken).ConfigureAwait(false);
                    readRegions.Add(UpstashRedisValue.FromString(resolvedReadRegion));
                }

                resolved.ReadRegions = readRegions;
            }
        }

        if (explicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Plan)))
        {
            resolved.Plan = source.Plan is null
                ? null
                : UpstashRedisValue.FromString(await ResolveOptionalStringAsync(source.Plan, "plan", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Budget)))
        {
            resolved.Budget = source.Budget is null
                ? null
                : UpstashRedisValue.FromString(await ResolveOptionalStringAsync(source.Budget, "budget", caller, executionContext, cancellationToken).ConfigureAwait(false));
        }

        if (explicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Eviction)))
        {
            resolved.Eviction = source.Eviction;
        }

        if (explicitSettings.Contains(nameof(UpstashRedisDeploymentOptions.Tls)))
        {
            resolved.Tls = source.Tls;
        }

        return resolved.ToProviderOptions();
    }

    private static async Task<string> ResolveRequiredStringAsync(
        UpstashRedisValue value,
        string settingName,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        string resolvedValue = await ResolveStringAsync(value, settingName, caller, executionContext, cancellationToken).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(resolvedValue)
            ? throw new InvalidOperationException($"Upstash Redis deployment requires a non-empty {settingName}.")
            : resolvedValue;
    }

    private static async Task<string> ResolveOptionalStringAsync(
        UpstashRedisValue value,
        string settingName,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        string resolvedValue = await ResolveStringAsync(value, settingName, caller, executionContext, cancellationToken).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(resolvedValue)
            ? throw new InvalidOperationException($"Upstash Redis {settingName} resolved to an empty value.")
            : resolvedValue;
    }

    private static async Task<string> ResolveStringAsync(
        UpstashRedisValue value,
        string settingName,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        if (value.LiteralValue is not null)
        {
            return value.LiteralValue;
        }

        ParameterResource parameter = value.Parameter
            ?? throw new InvalidOperationException($"Upstash Redis {settingName} is not backed by a literal value or an Aspire parameter.");

        try
        {
            if (executionContext is null)
            {
                string? parameterValue = await parameter.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return parameterValue
                    ?? throw new InvalidOperationException($"Upstash Redis {settingName} parameter '{parameter.Name}' resolved to null.");
            }

            ValueProviderContext valueProviderContext = new()
            {
                ExecutionContext = executionContext,
                Caller = caller
            };

            string? contextParameterValue = await parameter.GetValueAsync(valueProviderContext, cancellationToken).ConfigureAwait(false);
            return contextParameterValue
                ?? throw new InvalidOperationException($"Upstash Redis {settingName} parameter '{parameter.Name}' resolved to null.");
        }
        catch (MissingParameterValueException exception)
        {
            throw new InvalidOperationException(
                $"Upstash Redis deployment requires {settingName} parameter '{parameter.Name}'. Provide it through Aspire parameter configuration before deploying.",
                exception);
        }
    }
}
