namespace Sortiment.Core.Values;

/// <summary>
/// A product's value for an attribute. The set of value types is closed: <see cref="TextValue"/>,
/// <see cref="BooleanValue"/>, <see cref="NumberValue"/>, <see cref="DateValue"/>,
/// <see cref="SingleSelectValue"/>, <see cref="MultiSelectValue"/>, <see cref="MeasurementValue"/> and
/// <see cref="AssetCollectionValue"/>.
/// </summary>
/// <remarks>
/// Values are immutable and compared by value. A value on its own is not known to be valid: rules that depend on the
/// attribute, such as its type, maximum length or options, are checked by
/// <see cref="Attributes.AttributeDefinition.Validate"/>.
/// </remarks>
public abstract record AttributeValue
{
    private protected AttributeValue()
    {
    }
}
