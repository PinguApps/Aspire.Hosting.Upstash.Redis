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

internal sealed class UpstashRedisDatabaseDetails
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
    public string Endpoint
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("port")]
    public int Port
    {
        get;
        set;
    }

    [JsonPropertyName("password")]
    public string? Password
    {
        get;
        set;
    }

    [JsonPropertyName("tls")]
    public bool Tls
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

    [JsonPropertyName("type")]
    public string? Type
    {
        get;
        set;
    }

    [JsonPropertyName("budget")]
    public int? Budget
    {
        get;
        set;
    }

    [JsonPropertyName("eviction")]
    public bool? Eviction
    {
        get;
        set;
    }

    [JsonPropertyName("customer_id")]
    public string? CustomerId
    {
        get;
        set;
    }
}

internal sealed class UpstashRedisCreateDatabaseRequest
{
    [JsonPropertyName("database_name")]
    public required string DatabaseName
    {
        get;
        init;
    }

    [JsonPropertyName("platform")]
    public required string Platform
    {
        get;
        init;
    }

    [JsonPropertyName("primary_region")]
    public required string PrimaryRegion
    {
        get;
        init;
    }

    [JsonPropertyName("read_regions")]
    public IReadOnlyList<string>? ReadRegions
    {
        get;
        init;
    }

    [JsonPropertyName("plan")]
    public string? Plan
    {
        get;
        init;
    }

    [JsonPropertyName("budget")]
    public int? Budget
    {
        get;
        init;
    }

    [JsonPropertyName("eviction")]
    public bool? Eviction
    {
        get;
        init;
    }

    [JsonPropertyName("tls")]
    public bool? Tls
    {
        get;
        init;
    }
}

internal sealed class UpstashRedisUpdateRegionsRequest
{
    [JsonPropertyName("read_regions")]
    public required IReadOnlyList<string> ReadRegions
    {
        get;
        init;
    }
}

internal sealed class UpstashRedisChangePlanRequest
{
    [JsonPropertyName("plan_name")]
    public required string PlanName
    {
        get;
        init;
    }
}

internal sealed class UpstashRedisUpdateBudgetRequest
{
    [JsonPropertyName("budget")]
    public required int Budget
    {
        get;
        init;
    }
}

internal sealed class UpstashRedisReadinessPollingOptions
{
    public static UpstashRedisReadinessPollingOptions Default
    {
        get;
    } = new();

    public TimeSpan Timeout
    {
        get;
        init;
    } = TimeSpan.FromMinutes(2);

    public TimeSpan Delay
    {
        get;
        init;
    } = TimeSpan.FromSeconds(2);
}
