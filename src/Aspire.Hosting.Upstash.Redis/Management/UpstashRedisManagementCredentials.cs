using System.Net.Http.Headers;
using System.Text;

namespace Aspire.Hosting.Upstash.Redis.Management;

internal sealed class UpstashRedisManagementCredentials
{
    public UpstashRedisManagementCredentials(string accountEmail, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        AccountEmail = accountEmail;
        ApiKey = apiKey;
    }

    public string AccountEmail
    {
        get;
    }

    public string ApiKey
    {
        get;
    }

    public AuthenticationHeaderValue CreateAuthorizationHeader()
    {
        string credentialValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AccountEmail}:{ApiKey}"));

        return new AuthenticationHeaderValue("Basic", credentialValue);
    }
}
