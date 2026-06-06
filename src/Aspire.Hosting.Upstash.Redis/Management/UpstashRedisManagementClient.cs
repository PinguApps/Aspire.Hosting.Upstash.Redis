using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisManagementClient : IUpstashRedisManagementClient
{
    private const string DatabasesPath = "redis/databases";
    private readonly HttpClient _httpClient;
    private readonly UpstashRedisManagementCredentials _credentials;

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public UpstashRedisManagementClient(HttpClient httpClient, UpstashRedisManagementCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(credentials);

        _httpClient = httpClient;
        _credentials = credentials;

        _httpClient.BaseAddress ??= new Uri("https://api.upstash.com/v2/");
    }

    public async Task<IReadOnlyList<UpstashRedisDatabaseSummary>> ListDatabasesAsync(CancellationToken cancellationToken)
    {
        return await SendAsync<IReadOnlyList<UpstashRedisDatabaseSummary>>(
            HttpMethod.Get,
            DatabasesPath,
            requestBody: null,
            requireCredentials: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<UpstashRedisDatabaseDetails> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        return await SendAsync<UpstashRedisDatabaseDetails>(
            HttpMethod.Get,
            $"redis/database/{Uri.EscapeDataString(databaseId)}",
            requestBody: null,
            requireCredentials: true,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<UpstashRedisDatabaseDetails?> FindDatabaseByNameAsync(string databaseName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        IReadOnlyList<UpstashRedisDatabaseSummary> databases = await ListDatabasesAsync(cancellationToken).ConfigureAwait(false);
        List<UpstashRedisDatabaseSummary> matches = [.. databases.Where(database => database.DatabaseName == databaseName).Take(2)];

        if (matches.Count > 1)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                statusCode: null,
                $"Upstash Redis returned more than one database named '{databaseName}'.");
        }

        UpstashRedisDatabaseSummary? match = matches.SingleOrDefault();

        return match is null
            ? null
            : await GetDatabaseAsync(match.DatabaseId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UpstashRedisDatabaseDetails> CreateDatabaseAsync(
        UpstashRedisCreateDatabaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await SendAsync<UpstashRedisDatabaseDetails>(
            HttpMethod.Post,
            "redis/database",
            request,
            requireCredentials: false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateReadRegionsAsync(
        string databaseId,
        UpstashRedisUpdateRegionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(request);

        await SendOkAsync(
            HttpMethod.Post,
            $"redis/update-regions/{Uri.EscapeDataString(databaseId)}",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePlanAsync(
        string databaseId,
        UpstashRedisChangePlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(request);

        await SendOkAsync(
            HttpMethod.Post,
            $"redis/{Uri.EscapeDataString(databaseId)}/change-plan",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateBudgetAsync(
        string databaseId,
        UpstashRedisUpdateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(request);

        await SendOkAsync(
            HttpMethod.Patch,
            $"redis/update-budget/{Uri.EscapeDataString(databaseId)}",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetEvictionAsync(string databaseId, bool enabled, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

        string action = enabled ? "enable-eviction" : "disable-eviction";

        await SendOkAsync(
            HttpMethod.Post,
            $"redis/{action}/{Uri.EscapeDataString(databaseId)}",
            requestBody: null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<UpstashRedisDatabaseDetails> WaitUntilReadyAsync(
        string databaseId,
        UpstashRedisReadinessPollingOptions pollingOptions,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
        ArgumentNullException.ThrowIfNull(pollingOptions);

        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            UpstashRedisDatabaseDetails database = await GetDatabaseAsync(databaseId, cancellationToken).ConfigureAwait(false);

            if (IsReady(database))
            {
                return database;
            }

            if (stopwatch.Elapsed >= pollingOptions.Timeout)
            {
                throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.ProviderContract,
                    statusCode: null,
                    $"Upstash Redis database '{databaseId}' did not become active before the readiness timeout.");
            }

            await Task.Delay(pollingOptions.Delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsReady(UpstashRedisDatabaseDetails database)
    {
        return string.Equals(database.State, "active", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(database.ModifyingState);
    }

    private async Task SendOkAsync(
        HttpMethod method,
        string requestUri,
        object? requestBody,
        CancellationToken cancellationToken)
    {
        await SendAsync<string>(
            method,
            requestUri,
            requestBody,
            requireCredentials: false,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method,
        string requestUri,
        object? requestBody,
        bool requireCredentials,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, requestUri);
        request.Headers.Authorization = _credentials.CreateAuthorizationHeader();

        if (requestBody is not null)
        {
            request.Content = JsonContent.Create(requestBody, options: _serializerOptions);
        }

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateFailureException(response.StatusCode, responseContent);
        }

        if (typeof(TResponse) == typeof(string))
        {
            return (TResponse)(object)responseContent.Trim('"');
        }

        TResponse deserialized;

        try
        {
            deserialized = JsonSerializer.Deserialize<TResponse>(responseContent, _serializerOptions)
                ?? throw new UpstashRedisProviderException(
                    UpstashRedisProviderFailureKind.ProviderContract,
                    response.StatusCode,
                    "Upstash Redis returned an empty or unrecognized response body.");
        }
        catch (JsonException exception)
        {
            throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                response.StatusCode,
                "Upstash Redis returned an invalid JSON response body.",
                exception);
        }

        _ = requireCredentials && deserialized is UpstashRedisDatabaseDetails { Password: null or "" } details
            ? throw new UpstashRedisProviderException(
                UpstashRedisProviderFailureKind.ProviderContract,
                response.StatusCode,
                $"Upstash Redis returned database '{details.DatabaseId}' without credentials.")
            : true;

        return deserialized;
    }

    private UpstashRedisProviderException CreateFailureException(HttpStatusCode statusCode, string responseContent)
    {
        UpstashRedisProviderFailureKind failureKind = statusCode switch
        {
            HttpStatusCode.BadRequest => UpstashRedisProviderFailureKind.Validation,
            HttpStatusCode.Unauthorized => UpstashRedisProviderFailureKind.Authentication,
            HttpStatusCode.Forbidden => UpstashRedisProviderFailureKind.Authorization,
            HttpStatusCode.NotFound => UpstashRedisProviderFailureKind.NotFound,
            HttpStatusCode.TooManyRequests => UpstashRedisProviderFailureKind.RateLimited,
            HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable => UpstashRedisProviderFailureKind.Transient,
            _ => UpstashRedisProviderFailureKind.Unexpected,
        };

        string providerMessage = ExtractProviderMessage(responseContent);

        return new UpstashRedisProviderException(
            failureKind,
            statusCode,
            $"Upstash Redis management API request failed with {(int)statusCode} {statusCode}: {providerMessage}");
    }

    private string ExtractProviderMessage(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return "No provider response body was returned.";
        }

        string sanitizedContent = RedactSecrets(responseContent);

        try
        {
            using JsonDocument document = JsonDocument.Parse(sanitizedContent);

            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out JsonElement errorElement)
                && errorElement.ValueKind == JsonValueKind.String)
            {
                return errorElement.GetString() ?? "No provider error message was returned.";
            }
        }
        catch (JsonException)
        {
            return sanitizedContent;
        }

        return sanitizedContent;
    }

    private string RedactSecrets(string value)
    {
        return value.Replace(_credentials.ApiKey, "[redacted]", StringComparison.Ordinal);
    }
}
