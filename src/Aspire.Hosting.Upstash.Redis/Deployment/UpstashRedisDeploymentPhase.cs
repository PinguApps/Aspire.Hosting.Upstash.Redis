namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal enum UpstashRedisDeploymentPhase
{
    ResolvingConfiguration,
    LocatingDatabase,
    ValidatingImmutableDrift,
    CreatingDatabase,
    ReconcilingMutableSettings,
    RetrievingOutputs,
}
