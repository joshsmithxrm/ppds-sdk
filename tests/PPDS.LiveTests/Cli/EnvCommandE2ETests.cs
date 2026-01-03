using FluentAssertions;
using PPDS.LiveTests.Infrastructure;

namespace PPDS.LiveTests.Cli;

/// <summary>
/// E2E tests for ppds env commands.
/// These tests require valid credentials to interact with Global Discovery.
/// Tests only run on .NET 8.0 since CLI is spawned with --framework net8.0.
/// </summary>
public class EnvCommandE2ETests : CliE2ETestBase
{
    #region env list

    [CliE2EFact]
    public async Task EnvList_NoActiveProfile_Fails()
    {
        // Clear any existing profiles first
        await RunCliAsync("auth", "clear");

        var result = await RunCliAsync("env", "list");

        result.ExitCode.Should().NotBe(0);
        result.StdErr.Should().Contain("No active profile");
    }

    [CliE2EWithCredentials]
    public async Task EnvList_WithServicePrincipal_FailsWithClearError()
    {
        // Service principals cannot use Global Discovery - it requires interactive auth.
        // The CLI should fail fast with a clear error message, NOT hang.
        var profileName = GenerateTestProfileName();
        var createResult = await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        createResult.ExitCode.Should().Be(0, $"Profile creation failed: {createResult.StdErr}");
        await RunCliAsync("auth", "select", "--name", profileName);

        var result = await RunCliAsync("env", "list");

        // Should fail with clear error, NOT timeout
        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny(
            "interactive", "not supported", "ClientSecret", "service principal",
            "ppds env select", "ppds auth create");
    }

    #endregion

    #region env who

    [CliE2EWithCredentials]
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

    [CliE2EWithCredentials]
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

    [CliE2EWithCredentials]
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

    [CliE2EFact]
    public async Task EnvWho_NoActiveProfile_Fails()
    {
        await RunCliAsync("auth", "clear");

        var result = await RunCliAsync("env", "who");

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("No active profile", "profile");
    }

    #endregion

    #region env select

    [CliE2EWithCredentials]
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

    [CliE2EWithCredentials]
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

    [CliE2EFact]
    public async Task EnvSelect_MissingEnvironment_Fails()
    {
        var result = await RunCliAsync("env", "select");

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("--environment", "required");
    }

    #endregion

    #region org alias

    [CliE2EFact]
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
