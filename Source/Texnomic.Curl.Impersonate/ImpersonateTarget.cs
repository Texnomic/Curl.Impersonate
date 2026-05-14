using System.ComponentModel;

namespace Texnomic.Curl.Impersonate;

/// <summary>
/// Browser impersonation target for <c>libcurl-impersonate</c>. The selected
/// target controls the TLS handshake (cipher suites, extensions, ALPN, ALPS),
/// HTTP/2 SETTINGS frame, and HTTP/3 transport parameters, producing a network
/// fingerprint indistinguishable from the real browser. This bypasses
/// fingerprint-based bot detection (Cloudflare, Akamai, DataDome, PerimeterX)
/// without requiring browser automation, cookies, or JavaScript challenges.
/// </summary>
/// <remarks>
/// <para>
/// Pass any of the predefined static instances (e.g. <see cref="Chrome146"/>) to
/// <see cref="CurlHttpClient"/>'s constructor. Custom strings are accepted via
/// the implicit string conversion in case a new <c>libcurl-impersonate</c>
/// release exposes a target not yet listed here.
/// </para>
/// <para>
/// The list reflects the targets shipped with <c>libcurl-impersonate</c> at the
/// time of this release. Consult the upstream project for the canonical set.
/// </para>
/// </remarks>
[TypeConverter(typeof(ImpersonateTargetConverter))]
public readonly record struct ImpersonateTarget(string Value)
{
    /// <summary>Implicitly unwraps the underlying libcurl-impersonate target string.</summary>
    public static implicit operator string(ImpersonateTarget Target) => Target.Value;

    /// <summary>Wraps an arbitrary libcurl-impersonate target string.</summary>
    public static implicit operator ImpersonateTarget(string Value) => new(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    #region Chrome

    /// <summary>Chrome 99 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome99 = new("chrome99");
    /// <summary>Chrome 100 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome100 = new("chrome100");
    /// <summary>Chrome 101 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome101 = new("chrome101");
    /// <summary>Chrome 104 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome104 = new("chrome104");
    /// <summary>Chrome 107 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome107 = new("chrome107");
    /// <summary>Chrome 110 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome110 = new("chrome110");
    /// <summary>Chrome 116 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome116 = new("chrome116");
    /// <summary>Chrome 119 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome119 = new("chrome119");
    /// <summary>Chrome 120 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome120 = new("chrome120");
    /// <summary>Chrome 123 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome123 = new("chrome123");
    /// <summary>Chrome 124 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome124 = new("chrome124");
    /// <summary>Chrome 131 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome131 = new("chrome131");
    /// <summary>Chrome 133 desktop fingerprint (variant 'a').</summary>
    public static readonly ImpersonateTarget Chrome133 = new("chrome133a");
    /// <summary>Chrome 136 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome136 = new("chrome136");
    /// <summary>Chrome 142 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome142 = new("chrome142");
    /// <summary>Chrome 145 desktop fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome145 = new("chrome145");
    /// <summary>Chrome 146 desktop fingerprint (current default).</summary>
    public static readonly ImpersonateTarget Chrome146 = new("chrome146");

    #endregion

    #region Chrome Android

    /// <summary>Chrome 99 on Android fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome99Android = new("chrome99_android");
    /// <summary>Chrome 131 on Android fingerprint.</summary>
    public static readonly ImpersonateTarget Chrome131Android = new("chrome131_android");

    #endregion

    #region Edge

    /// <summary>Microsoft Edge 99 fingerprint.</summary>
    public static readonly ImpersonateTarget Edge99 = new("edge99");
    /// <summary>Microsoft Edge 101 fingerprint.</summary>
    public static readonly ImpersonateTarget Edge101 = new("edge101");

    #endregion

    #region Firefox

    /// <summary>Firefox 133 fingerprint.</summary>
    public static readonly ImpersonateTarget Firefox133 = new("firefox133");
    /// <summary>Firefox 135 fingerprint.</summary>
    public static readonly ImpersonateTarget Firefox135 = new("firefox135");
    /// <summary>Firefox 144 fingerprint.</summary>
    public static readonly ImpersonateTarget Firefox144 = new("firefox144");
    /// <summary>Firefox 147 fingerprint.</summary>
    public static readonly ImpersonateTarget Firefox147 = new("firefox147");

    #endregion

    #region Safari

    /// <summary>Safari 15.3 fingerprint.</summary>
    public static readonly ImpersonateTarget Safari153 = new("safari153");
    /// <summary>Safari 15.5 fingerprint.</summary>
    public static readonly ImpersonateTarget Safari155 = new("safari155");
    /// <summary>Safari 17.0 fingerprint.</summary>
    public static readonly ImpersonateTarget Safari170 = new("safari170");
    /// <summary>Safari 18.0 fingerprint.</summary>
    public static readonly ImpersonateTarget Safari180 = new("safari180");
    /// <summary>Safari 18.4 fingerprint.</summary>
    public static readonly ImpersonateTarget Safari184 = new("safari184");
    /// <summary>Safari 26.0 fingerprint.</summary>
    public static readonly ImpersonateTarget Safari260 = new("safari260");

    #endregion

    #region Safari iOS

    /// <summary>Safari 17.2 on iOS fingerprint.</summary>
    public static readonly ImpersonateTarget Safari172Ios = new("safari172_ios");
    /// <summary>Safari 18.0 on iOS fingerprint.</summary>
    public static readonly ImpersonateTarget Safari180Ios = new("safari180_ios");
    /// <summary>Safari 18.4 on iOS fingerprint.</summary>
    public static readonly ImpersonateTarget Safari184Ios = new("safari184_ios");
    /// <summary>Safari 26.0 on iOS fingerprint.</summary>
    public static readonly ImpersonateTarget Safari260Ios = new("safari260_ios");

    #endregion

    #region Tor

    /// <summary>Tor Browser 14.5 fingerprint.</summary>
    public static readonly ImpersonateTarget Tor145 = new("tor145");

    #endregion
}
