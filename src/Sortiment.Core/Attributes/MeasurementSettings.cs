using Sortiment.Core.Measurements;
using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings of <see cref="AttributeType.Measurement"/> attributes: the measurement family whose units their values
/// use.
/// </summary>
public sealed class MeasurementSettings : AttributeSettings
{
    internal MeasurementSettings(MeasurementFamily family) => Family = family;

    /// <summary>
    /// Gets the measurement family, which decides the units values can use. It cannot change after the attribute is
    /// created.
    /// </summary>
    public MeasurementFamily Family { get; }

    internal override IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value)
    {
        if (value is not MeasurementValue measurement)
        {
            return [ValueErrors.TypeMismatch(attribute, value)];
        }

        return Family.FindUnit(measurement.Unit) is null
            ? [ValueErrors.UnknownUnit(attribute, Family, measurement.Unit)]
            : [];
    }
}
