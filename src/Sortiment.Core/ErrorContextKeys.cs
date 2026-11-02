namespace Sortiment.Core;

/// <summary>
/// Well-known keys for <see cref="ResultError.Context"/>.
/// </summary>
/// <remarks>
/// Keys named after an entity, such as <see cref="Attribute"/>, always hold its code. Keys ending in <c>_id</c>, such
/// as <see cref="AttributeId"/>, hold its ID and are used where the code is not known (ADR 0012).
/// </remarks>
public static class ErrorContextKeys
{
    /// <summary>
    /// The code of the attribute the error relates to.
    /// </summary>
    public const string Attribute = "attribute";

    /// <summary>
    /// The ID of the attribute the error relates to, used where its code is not known.
    /// </summary>
    public const string AttributeId = "attribute_id";

    /// <summary>
    /// The code of the attribute option the error relates to.
    /// </summary>
    public const string Option = "option";

    /// <summary>
    /// The code of the channel the error relates to.
    /// </summary>
    public const string Channel = "channel";

    /// <summary>
    /// The ID of the channel the error relates to, used where its code is not known, such as in a value's scope.
    /// </summary>
    public const string ChannelId = "channel_id";

    /// <summary>
    /// The BCP 47 tag of the locale the error relates to.
    /// </summary>
    public const string Locale = "locale";

    /// <summary>
    /// The SKU of the product the error relates to.
    /// </summary>
    public const string Product = "product";

    /// <summary>
    /// The code of the family the error relates to.
    /// </summary>
    public const string Family = "family";

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
    /// The ID of the measurement unit the error relates to, where its code is not known.
    /// </summary>
    public const string UnitId = "unit_id";

    /// <summary>
    /// The invalid input value.
    /// </summary>
    public const string Value = "value";
}
