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
    private MeasurementFamily(MeasurementFamilyId id, MeasurementFamilyCode code, MeasurementUnit[] units)
    {
        Id = id;
        Code = code;
        Units = units.AsReadOnly();
    }

    /// <summary>
    /// Gets the identity of the family.
    /// </summary>
    public MeasurementFamilyId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the family.
    /// </summary>
    public MeasurementFamilyCode Code { get; }

    /// <summary>
    /// Gets the units of the family, in display order.
    /// </summary>
    public ReadOnlyCollection<MeasurementUnit> Units { get; }

    /// <summary>
    /// Creates a new measurement family.
    /// </summary>
    /// <param name="code">The family's code.</param>
    /// <param name="units">The family's units, in display order.</param>
    /// <returns>
    /// The new family, or the errors <c>measurement_family.unit_required</c> if no unit is given,
    /// <c>measurement_family.duplicate_unit</c> for each repeated unit code,
    /// <c>measurement_family.duplicate_unit_id</c> for each repeated unit ID and
    /// <c>measurement_family.invalid_conversion_factor</c> for each factor that is not greater than zero.
    /// </returns>
    public static Result<MeasurementFamily> Create(MeasurementFamilyCode code, IEnumerable<MeasurementUnit> units) =>
        Build(MeasurementFamilyId.New(), code, units);

    /// <summary>
    /// Restores a measurement family from stored state (ADR 0011).
    /// </summary>
    /// <param name="id">The family's ID.</param>
    /// <param name="code">The family's code.</param>
    /// <param name="units">The family's units, in display order.</param>
    /// <returns>The family.</returns>
    /// <exception cref="ArgumentException">
    /// The family has no units, a repeated unit code or ID, or a conversion factor that is not greater than zero.
    /// </exception>
    public static MeasurementFamily Restore(
        MeasurementFamilyId id,
        MeasurementFamilyCode code,
        IEnumerable<MeasurementUnit> units) =>
        CodeRules.ValueOrThrow(Build(id, code, units), nameof(units));

    private static Result<MeasurementFamily> Build(
        MeasurementFamilyId id,
        MeasurementFamilyCode code,
        IEnumerable<MeasurementUnit> units)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(units);

        var unitList = units.ToArray();
        var errors = new List<ResultError>();
        if (unitList.Length == 0)
        {
            errors.Add(MeasurementErrors.UnitRequired(code));
        }

        var seenCodes = new HashSet<UnitCode>();
        var seenIds = new HashSet<MeasurementUnitId>();
        foreach (var unit in unitList)
        {
            ArgumentNullException.ThrowIfNull(unit, nameof(units));
            if (!seenCodes.Add(unit.Code))
            {
                errors.Add(MeasurementErrors.DuplicateUnit(code, unit.Code));
            }

            if (!seenIds.Add(unit.Id))
            {
                errors.Add(MeasurementErrors.DuplicateUnitId(code, unit.Id));
            }

            if (unit.ConversionFactor <= 0)
            {
                errors.Add(MeasurementErrors.InvalidConversionFactor(code, unit));
            }
        }

        return errors.Count == 0
            ? Result.Success(new MeasurementFamily(id, code, unitList))
            : Result.Failure<MeasurementFamily>(errors);
    }

    /// <summary>
    /// Finds a unit by code, for example to resolve a unit given in an import or an API request.
    /// </summary>
    /// <param name="code">The unit's code.</param>
    /// <returns>The unit, or <see langword="null"/> if the family has no such unit.</returns>
    public MeasurementUnit? FindUnit(UnitCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Units.FirstOrDefault(unit => unit.Code == code);
    }

    /// <summary>
    /// Finds a unit by ID, for example the unit of a <see cref="MeasurementValue"/>.
    /// </summary>
    /// <param name="id">The unit's ID.</param>
    /// <returns>The unit, or <see langword="null"/> if the family has no such unit.</returns>
    public MeasurementUnit? FindUnit(MeasurementUnitId id) => Units.FirstOrDefault(unit => unit.Id == id);

    /// <summary>
    /// Converts a measurement to another unit of this family.
    /// </summary>
    /// <param name="value">The measurement to convert.</param>
    /// <param name="targetUnit">The ID of the unit to convert to.</param>
    /// <returns>
    /// The converted measurement, or the error <c>measurement_family.unknown_unit</c> for each unit that is not part
    /// of this family.
    /// </returns>
    public Result<MeasurementValue> Convert(MeasurementValue value, MeasurementUnitId targetUnit)
    {
        ArgumentNullException.ThrowIfNull(value);

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
