namespace Lipis.Flags.Avalonia;

/// <summary>
/// The shape a flag is drawn in.
/// </summary>
/// <remarks>
/// Both converters take this as their <c>ConverterParameter</c>, either as the enum value or as
/// one of the strings <c>"4x3"</c>, <c>"1x1"</c>, <c>"FourByThree"</c> or <c>"OneByOne"</c>.
/// Anything else selects <see cref="FourByThree"/>.
/// </remarks>
public enum FlagAspectRatio
{
    /// <summary>4:3, the usual rectangular flag. The default.</summary>
    FourByThree,

    /// <summary>1:1, square, for lists and icons.</summary>
    OneByOne,
}
