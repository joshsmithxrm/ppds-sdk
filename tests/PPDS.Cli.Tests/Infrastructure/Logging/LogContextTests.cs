using PPDS.Cli.Infrastructure.Logging;
using Xunit;

namespace PPDS.Cli.Tests.Infrastructure.Logging;

public class LogContextTests
{
    [Fact]
    public void Constructor_GeneratesCorrelationId()
    {
        var context = new LogContext();

        Assert.NotNull(context.CorrelationId);
        Assert.NotEmpty(context.CorrelationId);
    }

    [Fact]
    public void Constructor_CorrelationIdIsValidGuid()
    {
        var context = new LogContext();

        Assert.True(Guid.TryParse(context.CorrelationId, out _));
    }

    [Fact]
    public void Constructor_CommandNameIsNull()
    {
        var context = new LogContext();

        Assert.Null(context.CommandName);
    }

    [Fact]
    public void Constructor_SetsStartedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var context = new LogContext();
        var after = DateTimeOffset.UtcNow;

        Assert.True(context.StartedAt >= before);
        Assert.True(context.StartedAt <= after);
    }

    [Fact]
    public void CorrelationId_CanBeSet()
    {
        var context = new LogContext();
        var customId = "custom-correlation-id";

        context.CorrelationId = customId;

        Assert.Equal(customId, context.CorrelationId);
    }

    [Fact]
    public void CommandName_CanBeSet()
    {
        var context = new LogContext();

        context.CommandName = "auth create";

        Assert.Equal("auth create", context.CommandName);
    }

    [Fact]
    public void MultipleInstances_HaveUniqueCorrelationIds()
    {
        var context1 = new LogContext();
        var context2 = new LogContext();

        Assert.NotEqual(context1.CorrelationId, context2.CorrelationId);
    }

    [Fact]
    public void StartedAt_IsImmutable()
    {
        var context = new LogContext();
        var originalStartedAt = context.StartedAt;

        // Wait a tiny bit
        Thread.Sleep(1);

        Assert.Equal(originalStartedAt, context.StartedAt);
    }
}
