using Aspire.Hosting.Upstash.Redis.Management;

namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisReconciliationException : Exception
{
    public UpstashRedisReconciliationException()
    {
    }

    public UpstashRedisReconciliationException(string message)
        : base(message)
    {
    }

    public UpstashRedisReconciliationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UpstashRedisReconciliationException(
        string settingName,
        UpstashRedisProviderFailureKind failureKind,
        string message)
        : base(message)
    {
        SettingName = settingName;
        FailureKind = failureKind;
    }

    public UpstashRedisReconciliationException(
        string settingName,
        UpstashRedisProviderFailureKind failureKind,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        SettingName = settingName;
        FailureKind = failureKind;
    }

    public string SettingName { get; } = string.Empty;

    public UpstashRedisProviderFailureKind FailureKind { get; } = UpstashRedisProviderFailureKind.Unexpected;
}
