using Sortiment.Core.Measurements;

namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="Attributes.AttributeType.Measurement"/> attribute: an amount in a unit of the attribute's
/// measurement family.
/// </summary>
/// <remarks>
/// The unit is referred to by ID (ADR 0013). Imports and the API resolve unit codes with
/// <see cref="MeasurementFamily.FindUnit(UnitCode)"/>. Values are compared exactly as given: <c>1 kg</c> and
/// <c>1000 g</c> are different values. Use <see cref="MeasurementFamily.Convert"/> to compare or present them in the
/// same unit.
/// </remarks>
/// <param name="Amount">The amount.</param>
/// <param name="Unit">The ID of the unit the amount is expressed in.</param>
public sealed record MeasurementValue(decimal Amount, MeasurementUnitId Unit) : AttributeValue;
