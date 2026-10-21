using System.Collections.ObjectModel;
using Sortiment.Core.Values;

namespace Sortiment.Core.Measurements;

/// <summary>
/// A group of units that measure the same quantity and can be converted into each other, for example weight
/// (g, kg, lb, …). Sortiment ships with the families in <see cref="MeasurementFamilies"/>.
/// </summary>
/// <remarks>
/// Every unit has a conversion factor relative to the same reference unit, so conversion is linear. Quantities that
/// also need an offset, such as temperature, are not supported.
/// </remarks>
public sealed class MeasurementFamily
{
    private MeasurementFamily(MeasurementFamilyCode code, MeasurementUnit[] units)
    {
        Code = code;
        Units = units.AsReadOnly();
    }

    /// <summary>
    /// Gets the code of the family.
    /// </summary>
    public MeasurementFamilyCode Code { get; }

    /// <summary>
    /// Gets the units of the family, in display order.
    /// </summary>
    public ReadOnlyCollection<MeasurementUnit> Units { get; }

    /// <summary>
    /// Creates a measurement family.
    /// </summary>
    /// <param name="code">The family's code.</param>
    /// <param name="units">The family's units, in display order.</param>
    /// <returns>
    /// The family, or the errors <c>measurement_family.unit_required</c> if no unit is given,
    /// <c>measurement_family.duplicate_unit</c> for each repeated unit code and
    /// <c>measurement_family.invalid_conversion_factor</c> for each factor that is not greater than zero.
    /// </returns>
    public static Result<MeasurementFamily> Create(MeasurementFamilyCode code, IEnumerable<MeasurementUnit> units)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(units);

        var unitList = units.ToArray();
        var errors = new List<ResultError>();
        if (unitList.Length == 0)
        {
            errors.Add(MeasurementErrors.UnitRequired(code));
        }

        var seen = new HashSet<UnitCode>();
        foreach (var unit in unitList)
        {
            ArgumentNullException.ThrowIfNull(unit, nameof(units));
            if (!seen.Add(unit.Code))
            {
                errors.Add(MeasurementErrors.DuplicateUnit(code, unit.Code));
            }

            if (unit.ConversionFactor <= 0)
            {
                errors.Add(MeasurementErrors.InvalidConversionFactor(code, unit));
            }
        }

        return errors.Count == 0
            ? Result.Success(new MeasurementFamily(code, unitList))
            : Result.Failure<MeasurementFamily>(errors);
    }

    /// <summary>
    /// Finds a unit by code.
    /// </summary>
    /// <param name="code">The unit's code.</param>
    /// <returns>The unit, or <see langword="null"/> if the family has no such unit.</returns>
    public MeasurementUnit? FindUnit(UnitCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Units.FirstOrDefault(unit => unit.Code == code);
    }

    /// <summary>
    /// Converts a measurement to another unit of this family.
    /// </summary>
    /// <param name="value">The measurement to convert.</param>
    /// <param name="targetUnit">The unit to convert to.</param>
    /// <returns>
    /// The converted measurement, or the error <c>measurement_family.unknown_unit</c> for each unit that is not part
    /// of this family.
    /// </returns>
    public Result<MeasurementValue> Convert(MeasurementValue value, UnitCode targetUnit)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(targetUnit);

        var source = FindUnit(value.Unit);
        var target = FindUnit(targetUnit);
        var errors = new List<ResultError>();
        if (source is null)
        {
            errors.Add(MeasurementErrors.UnknownUnit(Code, value.Unit));
        }

        if (target is null)
        {
            errors.Add(MeasurementErrors.UnknownUnit(Code, targetUnit));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<MeasurementValue>(errors);
        }

        var amount = value.Unit == targetUnit
            ? value.Amount
            : value.Amount * source!.ConversionFactor / target!.ConversionFactor;
        return Result.Success(new MeasurementValue(amount, targetUnit));
    }
}
