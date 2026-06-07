namespace Aspire.Hosting.Upstash.Redis.Deployment;

internal enum UpstashRedisOwnershipResolutionFailureReason
{
    Unknown,
    CreateOnlyDatabaseAlreadyExists,
    ExistingOnlyDatabaseMissing,
    ExistingDatabaseIncompatible
}
