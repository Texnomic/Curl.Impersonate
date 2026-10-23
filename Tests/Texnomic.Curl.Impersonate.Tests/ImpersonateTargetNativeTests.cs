using System.Reflection;
using Xunit;

namespace Texnomic.Curl.Impersonate.Tests;

/// <summary>
/// Checks <see cref="ImpersonateTarget"/> against the bundled native library: every
/// field must name a target in libcurl-impersonate's table, so a stale or mistyped
/// value fails here instead of at request time. No network access.
/// </summary>
public class ImpersonateTargetNativeTests
{
    public static TheoryData<string, string> Targets()
    {
        var TheoryData = new TheoryData<string, string>();

        var Fields = typeof(ImpersonateTarget)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(Field => Field.FieldType == typeof(ImpersonateTarget));

        foreach (var Field in Fields)
            TheoryData.Add(Field.Name, ((ImpersonateTarget)Field.GetValue(null)!).Value);

        return TheoryData;
    }

    [Theory]
    [MemberData(nameof(Targets))]
    public void Field_IsAcceptedByBundledNativeLibrary(string Field, string Value)
    {
        // Initialises libcurl's global state through the wrapper's guarded path.
        using var Client = new CurlHttpClient();

        using var Handle = new CurlEasyHandle();

        var Error = Record.Exception(() => Handle.SetTarget(Value));

        Assert.True(Error is null, $"{Field} = \"{Value}\" was rejected: {Error?.Message}");
    }

    [Theory]
    [InlineData("chrome147")]
    [InlineData("Safari2601")]
    public void UnknownOrMiscasedTarget_IsRejected(string Value)
    {
        using var Client = new CurlHttpClient();

        using var Handle = new CurlEasyHandle();

        Assert.Throws<InvalidOperationException>(() => Handle.SetTarget(Value));
    }
}
