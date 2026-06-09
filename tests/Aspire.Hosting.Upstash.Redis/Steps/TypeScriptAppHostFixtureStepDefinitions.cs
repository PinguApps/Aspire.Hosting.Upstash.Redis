using Reqnroll;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PinguApps.Aspire.Hosting.Upstash.Redis.Tests.Steps;

[Binding]
public sealed class TypeScriptAppHostFixtureStepDefinitions
{
    private string? _fixtureDirectory;
    private string? _appHostSource;
    private JsonElement _packageJson;
    private CommandResult? _publishStepsResult;
    private bool _stoppedCleanly;

    [Given("the TypeScript AppHost fixture")]
    public void GivenTheTypeScriptAppHostFixture()
    {
        _fixtureDirectory = FindFixtureDirectory();
        string appHostPath = Path.Combine(_fixtureDirectory, "apphost.mts");
        string packageJsonPath = Path.Combine(_fixtureDirectory, "package.json");
        string aspireConfigPath = Path.Combine(_fixtureDirectory, "aspire.config.json");

        Assert.True(File.Exists(appHostPath), $"Expected fixture file '{appHostPath}' to exist.");
        Assert.True(File.Exists(packageJsonPath), $"Expected fixture file '{packageJsonPath}' to exist.");
        Assert.True(File.Exists(aspireConfigPath), $"Expected fixture file '{aspireConfigPath}' to exist.");

        _appHostSource = File.ReadAllText(appHostPath);
        using JsonDocument packageJson = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
        _packageJson = packageJson.RootElement.Clone();
    }

    [Then("the fixture imports the generated Aspire and Upstash Redis modules")]
    public void ThenTheFixtureImportsTheGeneratedAspireAndUpstashRedisModules()
    {
        string source = GetAppHostSource();

        Assert.Contains("from \"./.aspire/modules/aspire.mjs\"", source, StringComparison.Ordinal);
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
        Assert.Contains("const outputReferences = [", source, StringComparison.Ordinal);
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
    }

    [Then("the fixture builds and runs the AppHost")]
    public void ThenTheFixtureBuildsAndRunsTheAppHost()
    {
        string source = GetAppHostSource();

        Assert.Contains("const app = await builder.build();", source, StringComparison.Ordinal);
        Assert.Contains("await app.run();", source, StringComparison.Ordinal);
    }

    [Then("the fixture keeps generated Aspire modules out of source control")]
    public void ThenTheFixtureKeepsGeneratedAspireModulesOutOfSourceControl()
    {
        string fixtureDirectory = GetFixtureDirectory();
        string gitIgnorePath = Path.Combine(fixtureDirectory, ".gitignore");

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
        AssertScript("start", "aspire start --non-interactive --isolated");
    }

    [Then("the fixture package can wait for the local Redis resource")]
    public void ThenTheFixturePackageCanWaitForTheLocalRedisResource()
    {
        AssertScript("wait:cache", "aspire wait cache --status healthy --timeout 120 --non-interactive");
    }

    [Then("the fixture package can publish the TypeScript AppHost")]
    public void ThenTheFixturePackageCanPublishTheTypeScriptAppHost()
    {
        AssertScript("publish", "aspire publish --non-interactive --list-steps");
    }

    [Then("the fixture package can type-check the generated SDK surface")]
    public void ThenTheFixturePackageCanTypeCheckTheGeneratedSdkSurface()
    {
        AssertScript("typecheck", "tsc -p tsconfig.apphost.json --noEmit");
    }

    [When("the TypeScript AppHost fixture restores generated SDK modules")]
    public void WhenTheTypeScriptAppHostFixtureRestoresGeneratedSdkModules()
    {
        RequireExecutable("aspire", "Aspire CLI is required for TypeScript AppHost SDK generation.");

        RunCommand("aspire", ["restore", "--non-interactive"], TimeSpan.FromMinutes(2));
    }

    [Then("the generated TypeScript SDK exposes the Upstash Redis surface")]
    public void ThenTheGeneratedTypeScriptSdkExposesTheUpstashRedisSurface()
    {
        string modulesDirectory = Path.Combine(GetFixtureDirectory(), ".aspire", "modules");

        Assert.True(Directory.Exists(modulesDirectory), $"Expected Aspire restore to generate '{modulesDirectory}'.");

        string generatedSource = File.ReadAllText(Path.Combine(modulesDirectory, "aspire.mts"));

        Assert.Contains("publishToUpstash", generatedSource, StringComparison.Ordinal);
        Assert.Contains("getUpstashRedisOutputs", generatedSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisCloudPlatform", generatedSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisOwnershipMode", generatedSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisPlan", generatedSource, StringComparison.Ordinal);
        Assert.Contains("upstashRedisRegion", generatedSource, StringComparison.Ordinal);
    }

    [Then("the TypeScript AppHost fixture type-checks")]
    public void ThenTheTypeScriptAppHostFixtureTypeChecks()
    {
        RequireExecutable("npm", "npm is required to install and run the TypeScript compiler.");

        RunCommand("npm", ["install", "--no-audit", "--no-fund"], TimeSpan.FromMinutes(2));
        RunCommand("npm", ["run", "typecheck"], TimeSpan.FromMinutes(1));
    }

    [When("the TypeScript AppHost fixture starts locally until Redis is healthy")]
    public void WhenTheTypeScriptAppHostFixtureStartsLocallyUntilRedisIsHealthy()
    {
        RequireExecutable("aspire", "Aspire CLI is required to start a TypeScript AppHost.");
        RequireExecutable("docker", "Docker is required for the local Redis resource.");

        try
        {
            RunCommand("aspire", ["start", "--non-interactive", "--isolated"], TimeSpan.FromMinutes(3));
            RunCommand("aspire", ["wait", "cache", "--status", "healthy", "--timeout", "120", "--non-interactive"], TimeSpan.FromMinutes(3));
        }
        finally
        {
            CommandResult stopResult = RunCommand(
                "aspire",
                ["stop", "--non-interactive"],
                TimeSpan.FromMinutes(1),
                assertSuccess: false);

            _stoppedCleanly = stopResult.ExitCode == 0;
        }
    }

    [Then("the TypeScript AppHost fixture stopped cleanly")]
    public void ThenTheTypeScriptAppHostFixtureStoppedCleanly()
    {
        Assert.True(_stoppedCleanly, "Expected the TypeScript AppHost fixture to stop cleanly after local validation.");
    }

    [When("the TypeScript AppHost fixture lists publish steps")]
    public void WhenTheTypeScriptAppHostFixtureListsPublishSteps()
    {
        RequireExecutable("aspire", "Aspire CLI is required to validate TypeScript AppHost publish behavior.");

        _publishStepsResult = RunCommand(
            "aspire",
            ["publish", "--non-interactive", "--list-steps"],
            TimeSpan.FromMinutes(2));
    }

    [Then("the TypeScript AppHost publish step listing succeeds")]
    public void ThenTheTypeScriptAppHostPublishStepListingSucceeds()
    {
        CommandResult result = _publishStepsResult ?? throw new InvalidOperationException("Publish steps have not been listed.");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("publish", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    private void AssertScript(string scriptName, string expectedCommand)
    {
        JsonElement scripts = _packageJson.GetProperty("scripts");
        string? actualCommand = scripts.GetProperty(scriptName).GetString();

        Assert.Equal(expectedCommand, actualCommand);
    }

    private CommandResult RunCommand(string fileName, string[] arguments, TimeSpan timeout, bool assertSuccess = true)
    {
        string fixtureDirectory = GetFixtureDirectory();
        string resolvedFileName = FindExecutable(fileName) ?? fileName;

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = resolvedFileName,
                WorkingDirectory = fixtureDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };

        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(TimeSpan.FromSeconds(10));
            throw new TimeoutException($"Command '{fileName} {string.Join(' ', arguments)}' did not exit within {timeout}.");
        }

        CommandResult result = new(
            process.ExitCode,
            standardOutput.GetAwaiter().GetResult(),
            standardError.GetAwaiter().GetResult());

        if (assertSuccess && result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{fileName} {string.Join(' ', arguments)}' failed with exit code {result.ExitCode}.{Environment.NewLine}{result.Output}");
        }

        return result;
    }

    private static void RequireExecutable(string executableName, string skipReason)
    {
        if (FindExecutable(executableName) is not null)
        {
            return;
        }

        Assert.Skip(skipReason);
    }

    private static string? FindExecutable(string executableName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (string directory in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (string extension in GetWindowsExecutableExtensions())
                {
                    string windowsCandidate = Path.Combine(directory, $"{executableName}{extension}");
                    if (File.Exists(windowsCandidate))
                    {
                        return windowsCandidate;
                    }
                }
            }

            string candidate = Path.Combine(directory, executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string[] GetWindowsExecutableExtensions()
    {
        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");

        if (!string.IsNullOrWhiteSpace(pathExt))
        {
            return pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [".com", ".exe", ".bat", ".cmd"];
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

    private sealed class CommandResult
    {
        public CommandResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public int ExitCode { get; }

        public string StandardOutput { get; }

        public string StandardError { get; }

        public string Output => $"{StandardOutput}{Environment.NewLine}{StandardError}";
    }
}
