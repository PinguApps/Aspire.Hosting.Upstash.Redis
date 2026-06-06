using System.Text.Json.Serialization;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisDatabaseSummary
{
    [JsonPropertyName("database_id")]
    public string DatabaseId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("database_name")]
    public string DatabaseName
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string? Endpoint
    {
        get;
        set;
    }

    [JsonPropertyName("port")]
    public int? Port
    {
        get;
        set;
    }

    [JsonPropertyName("state")]
    public string? State
    {
        get;
        set;
    }

    [JsonPropertyName("modifying_state")]
    public string? ModifyingState
    {
        get;
        set;
    }

    [JsonPropertyName("primary_region")]
    public string? PrimaryRegion
    {
        get;
        set;
    }

    [JsonPropertyName("read_regions")]
    public IReadOnlyList<string>? ReadRegions
    {
        get;
        set;
    }
}
