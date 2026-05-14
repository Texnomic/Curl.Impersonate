using System.ComponentModel;
using Xunit;

namespace Texnomic.Curl.Impersonate.Tests;

public class ImpersonateTargetTests
{
    [Fact]
    public void ImplicitConversion_FromString_PreservesValue()
    {
        ImpersonateTarget Target = "chrome146";

        Assert.Equal("chrome146", Target.Value);
    }

    [Fact]
    public void ImplicitConversion_ToString_PreservesValue()
    {
        string Value = ImpersonateTarget.Chrome146;

        Assert.Equal("chrome146", Value);
    }

    [Fact]
    public void Chrome133_HasUpstreamSuffix()
    {
        Assert.Equal("chrome133a", ImpersonateTarget.Chrome133.Value);
    }

    [Theory]
    [InlineData("chrome99")]
    [InlineData("firefox144")]
    [InlineData("safari260_ios")]
    [InlineData("tor145")]
    public void TypeConverter_ConvertsStringToTarget(string Value)
    {
        var Converter = TypeDescriptor.GetConverter(typeof(ImpersonateTarget));

        var Result = (ImpersonateTarget)Converter.ConvertFromInvariantString(Value)!;

        Assert.Equal(Value, Result.Value);
    }

    [Fact]
    public void TypeConverter_ConvertsTargetToString()
    {
        var Converter = TypeDescriptor.GetConverter(typeof(ImpersonateTarget));

        var Result = Converter.ConvertToInvariantString(ImpersonateTarget.Firefox147);

        Assert.Equal("firefox147", Result);
    }
}
