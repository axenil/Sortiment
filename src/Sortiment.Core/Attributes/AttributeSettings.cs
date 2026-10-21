using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings that only apply to one kind of attribute, such as the maximum length of a text attribute or the
/// options of a select attribute. Every <see cref="AttributeDefinition"/> has the settings type that matches its
/// <see cref="AttributeDefinition.Type"/>.
/// </summary>
/// <remarks>
/// The set of settings types is closed: <see cref="TextSettings"/>, <see cref="BooleanSettings"/>,
/// <see cref="NumberSettings"/>, <see cref="DateSettings"/>, <see cref="SelectSettings"/>,
/// <see cref="MeasurementSettings"/> and <see cref="AssetCollectionSettings"/>.
/// </remarks>
public abstract class AttributeSettings
{
    private protected AttributeSettings()
    {
    }

    /// <summary>
    /// Checks a value's content: that it has the value type this kind of attribute holds, and that it respects
    /// these settings.
    /// </summary>
    internal abstract IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value);
}
