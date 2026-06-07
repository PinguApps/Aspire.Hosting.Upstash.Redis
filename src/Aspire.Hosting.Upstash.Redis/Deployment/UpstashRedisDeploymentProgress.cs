namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal sealed class UpstashRedisDeploymentProgress
{
    public UpstashRedisDeploymentProgress(
        UpstashRedisDeploymentPhase phase,
        string message,
        string? resourceName,
        string? databaseName,
        string? providerDatabaseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Phase = phase;
        Message = message;
        ResourceName = resourceName;
        DatabaseName = databaseName;
        ProviderDatabaseId = providerDatabaseId;
    }

    public UpstashRedisDeploymentPhase Phase { get; }

    public string Message { get; }

    public string? ResourceName { get; }

    public string? DatabaseName { get; }

    public string? ProviderDatabaseId { get; }
}
