using System.ComponentModel;
using System.Globalization;

namespace Texnomic.Curl.Impersonate;

internal sealed class ImpersonateTargetConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? Context, Type SourceType) =>
        SourceType == typeof(string) || base.CanConvertFrom(Context, SourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? Context, CultureInfo? Culture, object Value) =>
        Value is string S ? new ImpersonateTarget(S) : base.ConvertFrom(Context, Culture, Value);

    public override object? ConvertTo(ITypeDescriptorContext? Context, CultureInfo? Culture, object? Value, Type DestinationType) =>
        DestinationType == typeof(string) && Value is ImpersonateTarget Target
            ? Target.Value
            : base.ConvertTo(Context, Culture, Value, DestinationType);
}