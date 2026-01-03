using FluentAssertions;
using PPDS.Dataverse.BulkOperations;
using PPDS.Migration.Import;
using Xunit;

namespace PPDS.Migration.Tests.Import;

public class ImportOptionsTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = new ImportOptions();

        options.UseBulkApis.Should().BeTrue();
        options.BypassCustomPlugins.Should().Be(CustomLogicBypass.None);
        options.BypassPowerAutomateFlows.Should().BeFalse();
        options.ContinueOnError.Should().BeTrue();
        options.MaxParallelEntities.Should().Be(4);
        options.Mode.Should().Be(ImportMode.Upsert);
        options.SuppressDuplicateDetection.Should().BeFalse();
        options.UserMappings.Should().BeNull();
        options.RespectDisablePluginsSetting.Should().BeTrue();
        options.StripOwnerFields.Should().BeFalse();
        options.SkipMissingColumns.Should().BeFalse();
    }

    [Fact]
    public void MaxParallelEntities_CanBeSet()
    {
        var options = new ImportOptions { MaxParallelEntities = 8 };

        options.MaxParallelEntities.Should().Be(8);
    }

    [Fact]
    public void MaxParallelEntities_ThrowsWhenLessThanOne()
    {
        var options = new ImportOptions();

        var act = () => options.MaxParallelEntities = 0;

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxParallelEntities must be at least 1*");
    }

    [Fact]
    public void UseBulkApis_CanBeSet()
    {
        var options = new ImportOptions { UseBulkApis = false };

        options.UseBulkApis.Should().BeFalse();
    }

    [Fact]
    public void BypassCustomPlugins_CanBeSet()
    {
        var options = new ImportOptions { BypassCustomPlugins = CustomLogicBypass.All };

        options.BypassCustomPlugins.Should().Be(CustomLogicBypass.All);
    }

    [Fact]
    public void BypassPowerAutomateFlows_CanBeSet()
    {
        var options = new ImportOptions { BypassPowerAutomateFlows = true };

        options.BypassPowerAutomateFlows.Should().BeTrue();
    }

    [Fact]
    public void ContinueOnError_CanBeSet()
    {
        var options = new ImportOptions { ContinueOnError = false };

        options.ContinueOnError.Should().BeFalse();
    }

    [Fact]
    public void Mode_CanBeSet()
    {
        var options = new ImportOptions { Mode = ImportMode.Create };

        options.Mode.Should().Be(ImportMode.Create);
    }

    [Fact]
    public void SuppressDuplicateDetection_CanBeSet()
    {
        var options = new ImportOptions { SuppressDuplicateDetection = true };

        options.SuppressDuplicateDetection.Should().BeTrue();
    }

    [Fact]
    public void UserMappings_CanBeSet()
    {
        var userMappings = new PPDS.Migration.Models.UserMappingCollection();
        var options = new ImportOptions { UserMappings = userMappings };

        options.UserMappings.Should().BeSameAs(userMappings);
    }

    [Fact]
    public void RespectDisablePluginsSetting_CanBeSet()
    {
        var options = new ImportOptions { RespectDisablePluginsSetting = false };

        options.RespectDisablePluginsSetting.Should().BeFalse();
    }

    [Fact]
    public void StripOwnerFields_CanBeSet()
    {
        var options = new ImportOptions { StripOwnerFields = true };

        options.StripOwnerFields.Should().BeTrue();
    }

    [Fact]
    public void SkipMissingColumns_CanBeSet()
    {
        var options = new ImportOptions { SkipMissingColumns = true };

        options.SkipMissingColumns.Should().BeTrue();
    }
}

public class ImportModeTests
{
    [Fact]
    public void ImportMode_HasExpectedValues()
    {
        var create = ImportMode.Create;
        var update = ImportMode.Update;
        var upsert = ImportMode.Upsert;

        create.Should().Be(ImportMode.Create);
        update.Should().Be(ImportMode.Update);
        upsert.Should().Be(ImportMode.Upsert);
    }
}
