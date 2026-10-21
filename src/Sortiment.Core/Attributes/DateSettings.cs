using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings of <see cref="AttributeType.Date"/> attributes, which have no constraints.
/// </summary>
public sealed class DateSettings : AttributeSettings
{
    internal DateSettings()
    {
    }

    internal override IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value) =>
        value is DateValue ? [] : [ValueErrors.TypeMismatch(attribute, value)];
}
