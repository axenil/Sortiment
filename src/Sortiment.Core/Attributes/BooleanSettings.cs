using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings of <see cref="AttributeType.Boolean"/> attributes, which have no constraints.
/// </summary>
public sealed class BooleanSettings : AttributeSettings
{
    internal BooleanSettings()
    {
    }

    internal override IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value) =>
        value is BooleanValue ? [] : [ValueErrors.TypeMismatch(attribute, value)];
}
