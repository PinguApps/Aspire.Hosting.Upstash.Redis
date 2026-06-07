namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisImmutableDrift
{
    public UpstashRedisImmutableDrift(
        UpstashRedisImmutableDriftFailureReason failureReason,
        string settingName,
        string requestedValue,
        string actualValue,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingName);
        ArgumentNullException.ThrowIfNull(requestedValue);
        ArgumentNullException.ThrowIfNull(actualValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        FailureReason = failureReason;
        SettingName = settingName;
        RequestedValue = requestedValue;
        ActualValue = actualValue;
        Message = message;
    }

    public UpstashRedisImmutableDriftFailureReason FailureReason { get; }

    public string SettingName { get; }

    public string RequestedValue { get; }

    public string ActualValue { get; }

    public string Message { get; }
}
