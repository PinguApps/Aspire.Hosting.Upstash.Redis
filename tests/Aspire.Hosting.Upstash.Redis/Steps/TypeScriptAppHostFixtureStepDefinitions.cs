using Reqnroll;
using System.Text.Json;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class TypeScriptAppHostFixtureStepDefinitions
{
    private string? _fixtureDirectory;
    private string? _appHostSource;
    private JsonElement _packageJson;

    [Given("the TypeScript AppHost fixture")]
    public void GivenTheTypeScriptAppHostFixture()
    {
        _fixtureDirectory = FindFixtureDirectory();
        string appHostPath = Path.Combine(_fixtureDirectory, "apphost.ts");
        string packageJsonPath = Path.Combine(_fixtureDirectory, "package.json");

        Assert.True(File.Exists(appHostPath), $"Expected fixture file '{appHostPath}' to exist.");
        Assert.True(File.Exists(packageJsonPath), $"Expected fixture file '{packageJsonPath}' to exist.");

        _appHostSource = File.ReadAllText(appHostPath);
        using JsonDocument packageJson = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
        _packageJson = packageJson.RootElement.Clone();
    }

    [Then("the fixture imports the generated Aspire and Upstash Redis modules")]
    public void ThenTheFixtureImportsTheGeneratedAspireAndUpstashRedisModules()
    {
        string source = GetAppHostSource();

        Assert.Contains("from \"./.aspire/modules/aspire.mjs\"", source, StringComparison.Ordinal);
        Assert.Contains("from \"./.aspire/modules/pinguapps-aspire-hosting-upstash-redis.mjs\"", source, StringComparison.Ordinal);
        Assert.Contains("upstashRedisCloudPlatform", source, StringComparison.Ordinal);
        Assert.Contains("upstashRedisOwnershipMode", source, StringComparison.Ordinal);
        Assert.Contains("upstashRedisPlan", source, StringComparison.Ordinal);
        Assert.Contains("upstashRedisRegion", source, StringComparison.Ordinal);
    }

    [Then("the fixture creates a builder and Redis resource")]
    public void ThenTheFixtureCreatesABuilderAndRedisResource()
    {
        string source = GetAppHostSource();

        Assert.Contains("const builder = await createBuilder();", source, StringComparison.Ordinal);
        Assert.Contains("await builder.addRedis(\"cache\")", source, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"upstash-database-name\")", source, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"upstash-account-email\")", source, StringComparison.Ordinal);
        Assert.Contains("await builder.addParameter(\"upstash-api-key\", { secret: true })", source, StringComparison.Ordinal);
    }

    [Then("the fixture calls the generated Upstash Redis publish API with DTO options")]
    public void ThenTheFixtureCallsTheGeneratedUpstashRedisPublishApiWithDtoOptions()
    {
        string source = GetAppHostSource();

        Assert.Contains("cache.publishToUpstash(databaseName, accountEmail, apiKey", source, StringComparison.Ordinal);
        Assert.Contains("ownershipMode: upstashRedisOwnershipMode.createOrAdopt", source, StringComparison.Ordinal);
        Assert.Contains("platform: upstashRedisCloudPlatform.aws", source, StringComparison.Ordinal);
        Assert.Contains("primaryRegion: upstashRedisRegion.awsEuWest1", source, StringComparison.Ordinal);
        Assert.Contains("readRegions: [upstashRedisRegion.awsEuWest2]", source, StringComparison.Ordinal);
        Assert.Contains("plan: upstashRedisPlan.payAsYouGo", source, StringComparison.Ordinal);
        Assert.Contains("budget: 20", source, StringComparison.Ordinal);
        Assert.Contains("eviction: true", source, StringComparison.Ordinal);
        Assert.Contains("tls: true", source, StringComparison.Ordinal);
    }

    [Then("the fixture consumes the generated Upstash Redis outputs")]
    public void ThenTheFixtureConsumesTheGeneratedUpstashRedisOutputs()
    {
        string source = GetAppHostSource();

        Assert.Contains("const outputs = await cache.getUpstashRedisOutputs();", source, StringComparison.Ordinal);
        Assert.Contains("await outputs.endpoint()", source, StringComparison.Ordinal);
        Assert.Contains("await outputs.port()", source, StringComparison.Ordinal);
        Assert.Contains("await outputs.password()", source, StringComparison.Ordinal);
        Assert.Contains("await outputs.tls()", source, StringComparison.Ordinal);
        Assert.Contains("await outputs.databaseName()", source, StringComparison.Ordinal);
    }

    [Then("the fixture wires a standard Redis reference to a consuming resource")]
    public void ThenTheFixtureWiresAStandardRedisReferenceToAConsumingResource()
    {
        string source = GetAppHostSource();

        Assert.Contains("await builder.addContainer(\"worker\"", source, StringComparison.Ordinal);
        Assert.Contains("await worker.withReference(cache)", source, StringComparison.Ordinal);
        Assert.Contains("await worker.withEnvironment(\"UPSTASH_REDIS_ENDPOINT\"", source, StringComparison.Ordinal);
    }

    [Then("the fixture builds and runs the AppHost")]
    public void ThenTheFixtureBuildsAndRunsTheAppHost()
    {
        Assert.Contains("await builder.build().runAsync();", GetAppHostSource(), StringComparison.Ordinal);
    }

    [Then("the fixture keeps generated Aspire modules out of source control")]
    public void ThenTheFixtureKeepsGeneratedAspireModulesOutOfSourceControl()
    {
        string fixtureDirectory = GetFixtureDirectory();
        string modulesDirectory = Path.Combine(fixtureDirectory, ".aspire", "modules");
        string gitIgnorePath = Path.Combine(fixtureDirectory, ".gitignore");

        Assert.False(Directory.Exists(modulesDirectory), "Generated .aspire/modules content must not be checked in.");
        Assert.Contains(".aspire/", File.ReadAllText(gitIgnorePath), StringComparison.Ordinal);
    }

    [Then("the fixture package can restore generated SDK modules")]
    public void ThenTheFixturePackageCanRestoreGeneratedSdkModules()
    {
        AssertScript("restore", "aspire restore --non-interactive");
    }

    [Then("the fixture package can run the TypeScript AppHost locally")]
    public void ThenTheFixturePackageCanRunTheTypeScriptAppHostLocally()
    {
        AssertScript("start", "aspire start --non-interactive");
    }

    [Then("the fixture package can publish the TypeScript AppHost")]
    public void ThenTheFixturePackageCanPublishTheTypeScriptAppHost()
    {
        AssertScript("publish", "aspire publish --non-interactive");
    }

    [Then("the fixture package can type-check the generated SDK surface")]
    public void ThenTheFixturePackageCanTypeCheckTheGeneratedSdkSurface()
    {
        AssertScript("typecheck", "tsc --noEmit");
    }

    private void AssertScript(string scriptName, string expectedCommand)
    {
        JsonElement scripts = _packageJson.GetProperty("scripts");
        string? actualCommand = scripts.GetProperty(scriptName).GetString();

        Assert.Equal(expectedCommand, actualCommand);
    }

    private string GetFixtureDirectory()
    {
        return _fixtureDirectory ?? throw new InvalidOperationException("The TypeScript AppHost fixture has not been loaded.");
    }

    private string GetAppHostSource()
    {
        return _appHostSource ?? throw new InvalidOperationException("The TypeScript AppHost fixture has not been loaded.");
    }

    private static string FindFixtureDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "tests",
                "Aspire.Hosting.Upstash.Redis",
                "Fixtures",
                "TypeScriptAppHost");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find tests/Aspire.Hosting.Upstash.Redis/Fixtures/TypeScriptAppHost.");
    }
}
