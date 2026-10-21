namespace Sortiment.Core.Attributes;

/// <summary>
/// The kind of value an attribute holds. The set is closed: every type has a matching value type and validation rules
/// in Sortiment.Core. An attribute's type cannot change after it is created.
/// </summary>
public enum AttributeType
{
    /// <summary>
    /// A single line of text, for example a product name.
    /// </summary>
    Text = 1,

    /// <summary>
    /// Multi-line text, for example a product description.
    /// </summary>
    TextArea = 2,

    /// <summary>
    /// A yes/no value.
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// A number without a unit, for example a pack size. Whole numbers and decimals share this type.
    /// </summary>
    Number = 4,

    /// <summary>
    /// A calendar date without a time.
    /// </summary>
    Date = 5,

    /// <summary>
    /// One option from the attribute's list of options.
    /// </summary>
    SingleSelect = 6,

    /// <summary>
    /// Any number of options from the attribute's list of options.
    /// </summary>
    MultiSelect = 7,

    /// <summary>
    /// An identifier such as a GTIN, EAN or manufacturer part number.
    /// Identifiers are the same in every locale and channel, so they cannot be localizable or scopable.
    /// </summary>
    Identifier = 8,

    /// <summary>
    /// An amount in a unit of the attribute's measurement family, for example a weight of <c>1.2 kg</c>.
    /// Create measurement attributes with <see cref="AttributeDefinition.CreateMeasurement"/>.
    /// </summary>
    Measurement = 9,

    /// <summary>
    /// One or more assets from the attribute's asset family, in order, for example product images.
    /// Create asset collection attributes with <see cref="AttributeDefinition.CreateAssetCollection"/>.
    /// </summary>
    AssetCollection = 10,
}
