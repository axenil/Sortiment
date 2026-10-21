namespace Sortiment.Core;

/// <summary>
/// Well-known keys for <see cref="ResultError.Context"/>.
/// </summary>
public static class ErrorContextKeys
{
    /// <summary>
    /// The code of the attribute the error relates to.
    /// </summary>
    public const string Attribute = "attribute";

    /// <summary>
    /// The code of the attribute option the error relates to.
    /// </summary>
    public const string Option = "option";

    /// <summary>
    /// The channel the error relates to: its code, or its ID where only the ID is known.
    /// </summary>
    public const string Channel = "channel";

    /// <summary>
    /// The BCP 47 tag of the locale the error relates to.
    /// </summary>
    public const string Locale = "locale";

    /// <summary>
    /// The code of the asset family the error relates to.
    /// </summary>
    public const string AssetFamily = "asset_family";

    /// <summary>
    /// The code of the asset the error relates to.
    /// </summary>
    public const string Asset = "asset";

    /// <summary>
    /// The code of the measurement family the error relates to.
    /// </summary>
    public const string MeasurementFamily = "measurement_family";

    /// <summary>
    /// The code of the measurement unit the error relates to.
    /// </summary>
    public const string Unit = "unit";

    /// <summary>
    /// The invalid input value.
    /// </summary>
    public const string Value = "value";
}
