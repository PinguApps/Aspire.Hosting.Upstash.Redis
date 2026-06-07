namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal interface IUpstashRedisDeploymentProgressReporter
{
    public void Report(UpstashRedisDeploymentProgress progress);
}
