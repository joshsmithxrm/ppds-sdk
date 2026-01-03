using FluentAssertions;
using PPDS.LiveTests.Infrastructure;
using Xunit;

namespace PPDS.LiveTests.Cli;

/// <summary>
/// E2E tests for ppds data schema command.
/// Generates CMT-format schema files from Dataverse metadata.
/// </summary>
public class DataSchemaCommandE2ETests : CliE2ETestBase
{
    #region data schema

    [SkipIfNoClientSecret]
    public async Task DataSchema_SingleEntity_GeneratesValidSchema()
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

        var outputPath = GenerateTempFilePath(".xml");

        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account",
            "--output", outputPath);

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
        File.Exists(outputPath).Should().BeTrue("Schema file should be created");

        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("account");
        content.Should().Contain("<entity");
    }

    [SkipIfNoClientSecret]
    public async Task DataSchema_MultipleEntities_GeneratesSchemaWithAll()
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

        var outputPath = GenerateTempFilePath(".xml");

        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account,contact",
            "--output", outputPath);

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("account");
        content.Should().Contain("contact");
    }

    [SkipIfNoClientSecret]
    public async Task DataSchema_WithDisablePlugins_SetsFlag()
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

        var outputPath = GenerateTempFilePath(".xml");

        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account",
            "--output", outputPath,
            "--disable-plugins");

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");

        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("disableplugins");
    }

    [SkipIfNoClientSecret]
    public async Task DataSchema_WithProfileOption_UsesSpecifiedProfile()
    {
        var profileName = GenerateTestProfileName();
        await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        var outputPath = GenerateTempFilePath(".xml");

        // Use --profile instead of selecting the profile
        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account",
            "--output", outputPath,
            "--profile", profileName);

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
        File.Exists(outputPath).Should().BeTrue();
    }

    [SkipIfNoClientSecret]
    public async Task DataSchema_WithEnvironmentOverride_UsesSpecifiedEnvironment()
    {
        var profileName = GenerateTestProfileName();
        await RunCliAsync(
            "auth", "create",
            "--name", profileName,
            "--applicationId", Configuration.ApplicationId!,
            "--clientSecret", Configuration.ClientSecret!,
            "--tenant", Configuration.TenantId!,
            "--environment", Configuration.DataverseUrl!);

        var outputPath = GenerateTempFilePath(".xml");

        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account",
            "--output", outputPath,
            "--profile", profileName,
            "--environment", Configuration.DataverseUrl!);

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
    }

    #endregion

    #region Validation errors

    [Fact]
    public async Task DataSchema_MissingEntities_Fails()
    {
        var outputPath = GenerateTempFilePath(".xml");

        var result = await RunCliAsync(
            "data", "schema",
            "--output", outputPath);

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("--entities", "required");
    }

    [Fact]
    public async Task DataSchema_MissingOutput_Fails()
    {
        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account");

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("--output", "required");
    }

    [SkipIfNoClientSecret]
    public async Task DataSchema_InvalidEntity_Fails()
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

        var outputPath = GenerateTempFilePath(".xml");

        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "nonexistent_entity_xyz123",
            "--output", outputPath);

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("not found", "does not exist", "Error", "failed");
    }

    [Fact]
    public async Task DataSchema_InvalidOutputDirectory_Fails()
    {
        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account",
            "--output", @"C:\nonexistent\directory\schema.xml");

        result.ExitCode.Should().NotBe(0);
        (result.StdOut + result.StdErr).Should().ContainAny("directory", "does not exist", "Error");
    }

    #endregion

    #region JSON output

    [SkipIfNoClientSecret]
    public async Task DataSchema_JsonFormat_OutputsProgress()
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

        var outputPath = GenerateTempFilePath(".xml");

        var result = await RunCliAsync(
            "data", "schema",
            "--entities", "account",
            "--output", outputPath,
            "--output-format", "json");

        result.ExitCode.Should().Be(0, $"StdErr: {result.StdErr}");
        // JSON format should output progress as JSON
        result.StdOut.Should().Contain("{");
    }

    #endregion
}
