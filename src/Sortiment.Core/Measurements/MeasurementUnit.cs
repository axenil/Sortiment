namespace Sortiment.Core.Measurements;

/// <summary>
/// A unit within a <see cref="MeasurementFamily"/>, for example kilogram within weight.
/// </summary>
/// <param name="Id">The unit's ID, which measurement values use to refer to it.</param>
/// <param name="Code">The unit's code, unique within its family.</param>
/// <param name="ConversionFactor">
/// How many of the family's reference unit one of this unit equals. In the built-in weight family the reference unit
/// is the gram, so the kilogram has the factor 1000. Must be greater than zero.
/// </param>
public sealed record MeasurementUnit(MeasurementUnitId Id, UnitCode Code, decimal ConversionFactor);
