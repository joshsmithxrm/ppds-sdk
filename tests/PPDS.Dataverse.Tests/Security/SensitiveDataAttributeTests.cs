using System.Reflection;
using PPDS.Dataverse.Pooling;
using PPDS.Dataverse.Security;
using Xunit;

namespace PPDS.Dataverse.Tests.Security;

public class SensitiveDataAttributeTests
{
    [Fact]
    public void DataverseConnection_ClientSecret_HasSensitiveDataAttribute()
    {
        var property = typeof(DataverseConnection).GetProperty(nameof(DataverseConnection.ClientSecret));
        var attribute = property?.GetCustomAttribute<SensitiveDataAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("Contains client secret", attribute.Reason);
        Assert.Equal("Secret", attribute.DataType);
    }

    [Fact]
    public void SensitiveDataAttribute_CanBeConstructedWithReason()
    {
        var attribute = new SensitiveDataAttribute("Test reason");

        Assert.Equal("Test reason", attribute.Reason);
    }

    [Fact]
    public void SensitiveDataAttribute_CanSetProperties()
    {
        var attribute = new SensitiveDataAttribute
        {
            Reason = "Contains secrets",
            DataType = "ApiKey"
        };

        Assert.Equal("Contains secrets", attribute.Reason);
        Assert.Equal("ApiKey", attribute.DataType);
    }

    [Fact]
    public void SensitiveDataAttribute_AllowsInheritance()
    {
        var attributeUsage = typeof(SensitiveDataAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(attributeUsage);
        Assert.True(attributeUsage.Inherited);
    }

    [Fact]
    public void SensitiveDataAttribute_DisallowsMultiple()
    {
        var attributeUsage = typeof(SensitiveDataAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(attributeUsage);
        Assert.False(attributeUsage.AllowMultiple);
    }

    [Fact]
    public void SensitiveDataAttribute_CanBeAppliedToPropertiesFieldsAndParameters()
    {
        var attributeUsage = typeof(SensitiveDataAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(attributeUsage);
        Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
        Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Field));
        Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Parameter));
    }
}
