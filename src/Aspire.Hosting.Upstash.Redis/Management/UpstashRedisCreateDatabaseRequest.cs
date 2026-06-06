using System.Text.Json.Serialization;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisCreateDatabaseRequest
{
    [JsonPropertyName("database_name")]
    public required string DatabaseName { get; init; }

    [JsonPropertyName("platform")]
    public required string Platform { get; init; }

    [JsonPropertyName("primary_region")]
    public required string PrimaryRegion { get; init; }

    [JsonPropertyName("read_regions")]
    public IReadOnlyList<string>? ReadRegions { get; init; }

    [JsonPropertyName("plan")]
    public string? Plan { get; init; }

    [JsonPropertyName("budget")]
    public int? Budget { get; init; }

    [JsonPropertyName("eviction")]
    public bool? Eviction { get; init; }

    [JsonPropertyName("tls")]
    public bool? Tls { get; init; }
}
