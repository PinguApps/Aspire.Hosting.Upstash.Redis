using Aspire.Hosting;
using PinguApps.Aspire.Hosting.Upstash.Redis.Samples;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class AppHostSnippetsStepDefinitions
{
    private IReadOnlyList<string> _sampleMethodNames = [];
    private string _typeScriptDemoSource = string.Empty;

    [When("the sample AppHost snippets are loaded")]
    public void WhenTheSampleAppHostSnippetsAreLoaded()
    {
        Action<IDistributedApplicationBuilder>[] snippets =
        [
            UpstashRedisAppHostSnippets.ConfigureCreateOrAdopt,
            UpstashRedisAppHostSnippets.ConfigureCreateOnly,
            UpstashRedisAppHostSnippets.ConfigureExistingOnly,
            UpstashRedisAppHostSnippets.ConfigureParameterizedOptions,
            UpstashRedisAppHostSnippets.ConfigureSupplementaryOutputConsumer,
        ];

        foreach (Action<IDistributedApplicationBuilder> snippet in snippets)
        {
            snippet(DistributedApplication.CreateBuilder());
        }

        _sampleMethodNames = snippets
            .Select(snippet => snippet.Method.Name)
            .ToArray();
    }

    [Then("the sample AppHost snippets cover the documented usage patterns")]
    public void ThenTheSampleAppHostSnippetsCoverTheDocumentedUsagePatterns()
    {
        Assert.Contains(nameof(UpstashRedisAppHostSnippets.ConfigureCreateOrAdopt), _sampleMethodNames);
        Assert.Contains(nameof(UpstashRedisAppHostSnippets.ConfigureCreateOnly), _sampleMethodNames);
        Assert.Contains(nameof(UpstashRedisAppHostSnippets.ConfigureExistingOnly), _sampleMethodNames);
        Assert.Contains(nameof(UpstashRedisAppHostSnippets.ConfigureParameterizedOptions), _sampleMethodNames);
        Assert.Contains(nameof(UpstashRedisAppHostSnippets.ConfigureSupplementaryOutputConsumer), _sampleMethodNames);
    }

    [When("the TypeScript demo AppHost source is loaded")]
    public void WhenTheTypeScriptDemoAppHostSourceIsLoaded()
    {
        string repositoryRoot = FindRepositoryRoot();
        string demoPath = Path.Combine(repositoryRoot, "samples", "TypeScriptAppHost", "apphost.ts");

        Assert.True(File.Exists(demoPath), $"Expected TypeScript demo AppHost '{demoPath}' to exist.");

        _typeScriptDemoSource = File.ReadAllText(demoPath);
    }

    [Then("the TypeScript demo AppHost uses the documented generated API")]
    public void ThenTheTypeScriptDemoAppHostUsesTheDocumentedGeneratedApi()
    {
        Assert.Contains("from \"./.modules/aspire.js\"", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisCloudPlatform", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisOwnershipMode", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisPlan", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisRegion", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addRedis(\"cache\")", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"upstash-database-name\")", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"upstash-account-email\")", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"upstash-api-key\", { secret: true })", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("cache.publishToUpstash(databaseName, accountEmail, apiKey", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("ownershipMode: upstashRedisOwnershipMode.createOrAdopt", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("platform: upstashRedisCloudPlatform.aws", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("primaryRegion: upstashRedisRegion.awsEuWest1", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("plan: upstashRedisPlan.payAsYouGo", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("eviction: true", _typeScriptDemoSource, StringComparison.Ordinal);
        Assert.Contains("await worker.withReference(cache)", _typeScriptDemoSource, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Aspire.Hosting.Upstash.Redis.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root.");
    }
}
