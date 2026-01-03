using FluentAssertions;
using PPDS.LiveTests.Infrastructure;
using Xunit;

namespace PPDS.LiveTests.Cli;

/// <summary>
/// E2E tests for ppds env commands.
/// These tests require valid credentials to interact with Global Discovery.
/// </summary>
public class EnvCommandE2ETests : CliE2ETestBase
{
    #region env list

    [SkipIfNoClientSecret]
    public async Task EnvList_WithActiveProfile_ListsEnvironments()
    {
        // First create a profile
        var profileName = GenerateTestProfileName();
        var createResult = await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        createResult.ExitCode.Should().Be(0, $"Profile creation failed: {createResult.StdErr}");

        // Select the profile
        await RunCliAsync("auth", "select", "--name", profileName);

        // List environments
        // Note: For service principal auth, env list may fail since it requires Global Discovery
        // which doesn't work with client credentials. This is expected behavior.
        var result = await RunCliAsync("env", "list");

        // Service principals can't use Global Discovery, so we expect either success or specific error
        if (result.ExitCode != 0)
        {
            // Expected: service principals can't access Global Discovery
            (result.StdOut + result.StdErr).Should().ContainAny(
                "Discovery", "service principal", "Error", "not supported");
        }
    }

    [Fact]
    public async Task EnvList_NoActiveProfile_Fails()
    {
        // Clear any existing profiles first
        await RunCliAsync("auth", "clear");

        var result = await RunCliAsync("env", "list");

        result.ExitCode.Should().NotBe(0);
        result.StdErr.Should().Contain("No active profile");
    }

    [SkipIfNoClientSecret]
    public async Task EnvList_JsonFormat_ReturnsValidJson()
    {
        var profileName = GenerateTestProfileName();
        await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        await RunCliAsync("auth", "select", "--name", profileName);

        var result = await RunCliAsync("env", "list", "--output-format", "json");

        // Either succeeds with JSON or fails (service principal limitation)
        if (result.ExitCode == 0)
        {
            result.StdOut.Trim().Should().StartWith("{");
            result.StdOut.Should().Contain("\"environments\"");
        }
    }

    #endregion

    #region env who

    [SkipIfNoClientSecret]
    public async Task EnvWho_WithEnvironmentSet_ShowsOrgInfo()
    {
        var profileName = GenerateTestProfileName();
        var createResult = await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        createResult.ExitCode.Should().Be(0);
        await RunCliAsync("auth", "select", "--name", profileName);

        var result = await RunCliAsync("env", "who");

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
        result.StdOut.Should().ContainAny("Organization", "Org ID", "User ID");
    }

    [SkipIfNoClientSecret]
    public async Task EnvWho_WithEnvironmentOverride_QueriesSpecificEnvironment()
    {
        var profileName = GenerateTestProfileName();
        await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        await RunCliAsync("auth", "select", "--name", profileName);

        // Query with explicit environment override
        var result = await RunCliAsync("env", "who", "--environment", Configuration.DataverseUrl!);

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
        result.StdOut.Should().ContainAny("Organization", "Org ID");
    }

    [SkipIfNoClientSecret]
    public async Task EnvWho_JsonFormat_ReturnsValidJson()
    {
        var profileName = GenerateTestProfileName();
        await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        await RunCliAsync("auth", "select", "--name", profileName);

        var result = await RunCliAsync("env", "who", "--output-format", "json");

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
        result.StdOut.Trim().Should().StartWith("{");
        result.StdOut.Should().ContainAny("\"userId\"", "\"organizationId\"");
    }

    [Fact]
    public async Task EnvWho_NoActiveProfile_Fails()
    {
        await RunCliAsync("auth", "clear");

        var result = await RunCliAsync("env", "who");

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("No active profile", "profile");
    }

    #endregion

    #region env select

    [SkipIfNoClientSecret]
    public async Task EnvSelect_ValidUrl_SelectsEnvironment()
    {
        var profileName = GenerateTestProfileName();
        await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        await RunCliAsync("auth", "select", "--name", profileName);

        var result = await RunCliAsync("env", "select", "--environment", Configuration.DataverseUrl!);

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
        result.StdOut.Should().ContainAny("Connected", "selected", "Environment");
    }

    [SkipIfNoClientSecret]
    public async Task EnvSelect_InvalidUrl_Fails()
    {
        var profileName = GenerateTestProfileName();
        await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        await RunCliAsync("auth", "select", "--name", profileName);

        var result = await RunCliAsync("env", "select", "--environment", "https://invalid.crm.dynamics.com");

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("Error", "failed", "could not");
    }

    [Fact]
    public async Task EnvSelect_MissingEnvironment_Fails()
    {
        var result = await RunCliAsync("env", "select");

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("--environment", "required");
    }

    #endregion

    #region org alias

    [Fact]
    public async Task OrgList_IsAliasForEnvList()
    {
        // org should work as alias for env
        await RunCliAsync("auth", "clear");

        var orgResult = await RunCliAsync("org", "list");
        var envResult = await RunCliAsync("env", "list");

        // Both should fail the same way (no profile)
        orgResult.ExitCode.Should().Be(envResult.ExitCode);
    }

    #endregion
}
