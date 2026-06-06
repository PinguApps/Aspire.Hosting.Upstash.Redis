using System.Text.Json.Serialization;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisUpdateRegionsRequest
{
    [JsonPropertyName("read_regions")]
    public required IReadOnlyList<string> ReadRegions { get; init; }
}
