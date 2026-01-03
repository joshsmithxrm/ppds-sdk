using FluentAssertions;
using PPDS.Migration.Export;
using PPDS.Migration.Progress;
using Xunit;

namespace PPDS.Migration.Tests.Export;

public class ExportResultTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var result = new ExportResult();

        result.Success.Should().BeFalse();
        result.EntitiesExported.Should().Be(0);
        result.RecordsExported.Should().Be(0);
        result.Duration.Should().Be(TimeSpan.Zero);
        result.EntityResults.Should().NotBeNull().And.BeEmpty();
        result.OutputPath.Should().BeNull();
        result.Errors.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var duration = TimeSpan.FromMinutes(3);
        var entityResults = new List<EntityExportResult> { new() { EntityLogicalName = "account" } };
        var errors = new List<MigrationError> { new() { Message = "Test error" } };

        var result = new ExportResult
        {
            Success = true,
            EntitiesExported = 5,
            RecordsExported = 2000,
            Duration = duration,
            EntityResults = entityResults,
            OutputPath = "C:\\temp\\export.zip",
            Errors = errors
        };

        result.Success.Should().BeTrue();
        result.EntitiesExported.Should().Be(5);
        result.RecordsExported.Should().Be(2000);
        result.Duration.Should().Be(duration);
        result.EntityResults.Should().HaveCount(1);
        result.OutputPath.Should().Be("C:\\temp\\export.zip");
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void RecordsPerSecond_CalculatesCorrectly()
    {
        var result = new ExportResult
        {
            RecordsExported = 1800,
            Duration = TimeSpan.FromSeconds(9)
        };

        result.RecordsPerSecond.Should().Be(200);
    }

    [Fact]
    public void RecordsPerSecond_ReturnsZeroWhenDurationIsZero()
    {
        var result = new ExportResult
        {
            RecordsExported = 1000,
            Duration = TimeSpan.Zero
        };

        result.RecordsPerSecond.Should().Be(0);
    }
}

public class EntityExportResultTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var result = new EntityExportResult();

        result.EntityLogicalName.Should().BeEmpty();
        result.RecordCount.Should().Be(0);
        result.Duration.Should().Be(TimeSpan.Zero);
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var duration = TimeSpan.FromSeconds(45);

        var result = new EntityExportResult
        {
            EntityLogicalName = "contact",
            RecordCount = 300,
            Duration = duration,
            Success = false,
            ErrorMessage = "Test error message"
        };

        result.EntityLogicalName.Should().Be("contact");
        result.RecordCount.Should().Be(300);
        result.Duration.Should().Be(duration);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test error message");
    }
}
