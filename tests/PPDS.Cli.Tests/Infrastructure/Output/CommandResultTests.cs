using PPDS.Cli.Infrastructure.Errors;
using PPDS.Cli.Infrastructure.Output;
using Xunit;

namespace PPDS.Cli.Tests.Infrastructure.Output;

public class CommandResultTests
{
    [Fact]
    public void Ok_SetsSuccessTrue()
    {
        var result = CommandResult<string>.Ok("test data");

        Assert.True(result.Success);
    }

    [Fact]
    public void Ok_SetsData()
    {
        var result = CommandResult<string>.Ok("test data");

        Assert.Equal("test data", result.Data);
    }

    [Fact]
    public void Ok_ErrorIsNull()
    {
        var result = CommandResult<string>.Ok("test data");

        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_WithStructuredError_SetsSuccessFalse()
    {
        var error = new StructuredError(ErrorCodes.Auth.ProfileNotFound, "Not found");
        var result = CommandResult<string>.Fail(error);

        Assert.False(result.Success);
    }

    [Fact]
    public void Fail_WithStructuredError_SetsError()
    {
        var error = new StructuredError(ErrorCodes.Auth.ProfileNotFound, "Not found");
        var result = CommandResult<string>.Fail(error);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Fail_WithStructuredError_DataIsNull()
    {
        var error = new StructuredError(ErrorCodes.Auth.ProfileNotFound, "Not found");
        var result = CommandResult<string>.Fail(error);

        Assert.Null(result.Data);
    }

    [Fact]
    public void Fail_WithCodeAndMessage_SetsSuccessFalse()
    {
        var result = CommandResult<string>.Fail(ErrorCodes.Operation.Internal, "Internal error");

        Assert.False(result.Success);
    }

    [Fact]
    public void Fail_WithCodeAndMessage_CreatesError()
    {
        var result = CommandResult<string>.Fail(ErrorCodes.Operation.Internal, "Internal error");

        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.Operation.Internal, result.Error.Code);
        Assert.Equal("Internal error", result.Error.Message);
    }

    [Fact]
    public void Fail_WithCodeMessageAndTarget_SetsTarget()
    {
        var result = CommandResult<string>.Fail(ErrorCodes.Validation.FileNotFound, "Not found", "schema.xml");

        Assert.NotNull(result.Error);
        Assert.Equal("schema.xml", result.Error.Target);
    }

    [Fact]
    public void Partial_SetsSuccessFalse()
    {
        var results = new List<ItemResult>
        {
            ItemResult.Ok("item1", null),
            ItemResult.Fail("item2", new StructuredError(ErrorCodes.Operation.NotFound, "Not found"))
        };

        var result = CommandResult<string>.Partial("partial data", results);

        Assert.False(result.Success);
    }

    [Fact]
    public void Partial_SetsData()
    {
        var results = new List<ItemResult> { ItemResult.Ok("item1", null) };

        var result = CommandResult<string>.Partial("partial data", results);

        Assert.Equal("partial data", result.Data);
    }

    [Fact]
    public void Partial_SetsResults()
    {
        var results = new List<ItemResult>
        {
            ItemResult.Ok("item1", null),
            ItemResult.Fail("item2", new StructuredError(ErrorCodes.Operation.NotFound, "Not found"))
        };

        var result = CommandResult<string>.Partial("partial data", results);

        Assert.NotNull(result.Results);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public void Ok_WithComplexType_SetsData()
    {
        var data = new { Name = "test", Value = 42 };
        var result = CommandResult<object>.Ok(data);

        Assert.True(result.Success);
        Assert.Equal(data, result.Data);
    }

    [Fact]
    public void Ok_WithNullData_IsAllowed()
    {
        var result = CommandResult<string?>.Ok(null);

        Assert.True(result.Success);
        Assert.Null(result.Data);
    }
}
