using System.Text.Json.Serialization;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisChangePlanRequest
{
    [JsonPropertyName("plan_name")]
    public required string PlanName { get; init; }
}
