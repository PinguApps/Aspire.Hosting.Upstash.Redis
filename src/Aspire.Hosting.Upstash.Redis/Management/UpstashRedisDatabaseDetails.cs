using System.Text.Json.Serialization;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisDatabaseDetails
{
    [JsonPropertyName("database_id")]
    public string DatabaseId { get; set; } = string.Empty;

    [JsonPropertyName("database_name")]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("tls")]
    public bool Tls { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("modifying_state")]
    public string? ModifyingState { get; set; }

    [JsonPropertyName("primary_region")]
    public string? PrimaryRegion { get; set; }

    [JsonPropertyName("read_regions")]
    public IReadOnlyList<string>? ReadRegions { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("db_disk_threshold")]
    public long? DbDiskThreshold { get; set; }

    [JsonPropertyName("budget")]
    public int? Budget { get; set; }

    [JsonPropertyName("eviction")]
    public bool? Eviction { get; set; }

    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }
}
