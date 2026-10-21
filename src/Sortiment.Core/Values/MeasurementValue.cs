using Sortiment.Core.Measurements;

namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="Attributes.AttributeType.Measurement"/> attribute: an amount in a unit of the attribute's
/// measurement family.
/// </summary>
/// <remarks>
/// Values are compared exactly as given: <c>1 kg</c> and <c>1000 g</c> are different values.
/// Use <see cref="MeasurementFamily.Convert"/> to compare or present them in the same unit.
/// </remarks>
public sealed record MeasurementValue : AttributeValue
{
    /// <summary>
    /// Creates a measurement value.
    /// </summary>
    /// <param name="amount">The amount.</param>
    /// <param name="unit">The unit the amount is expressed in.</param>
    public MeasurementValue(decimal amount, UnitCode unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        Amount = amount;
        Unit = unit;
    }

    /// <summary>
    /// Gets the amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the unit the amount is expressed in.
    /// </summary>
    public UnitCode Unit { get; }
}
