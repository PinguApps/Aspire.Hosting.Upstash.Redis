using Aspire.Hosting;
using PinguApps.Aspire.Hosting.Upstash.Redis.Samples;
using Reqnroll;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class AppHostSnippetsStepDefinitions
{
    private IReadOnlyList<string> _sampleMethodNames = [];

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
}
