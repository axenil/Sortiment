using System.Collections.ObjectModel;

namespace Sortiment.Core.Measurements;

/// <summary>
/// The measurement families that ship with Sortiment. Their codes are reserved.
/// </summary>
/// <remarks>
/// All conversion factors are exact: imperial and US units use their legal definitions in metric units.
/// </remarks>
public static class MeasurementFamilies
{
    /// <summary>
    /// Gets the weight family: <c>mg</c>, <c>g</c>, <c>kg</c>, <c>oz</c> and <c>lb</c>. The reference unit is the gram.
    /// </summary>
    public static MeasurementFamily Weight { get; } = Define(
        "weight",
        ("mg", 0.001m),
        ("g", 1m),
        ("kg", 1000m),
        ("oz", 28.349523125m),
        ("lb", 453.59237m));

    /// <summary>
    /// Gets the length family: <c>mm</c>, <c>cm</c>, <c>m</c>, <c>in</c> and <c>ft</c>. The reference unit is the
    /// millimeter.
    /// </summary>
    public static MeasurementFamily Length { get; } = Define(
        "length",
        ("mm", 1m),
        ("cm", 10m),
        ("m", 1000m),
        ("in", 25.4m),
        ("ft", 304.8m));

    /// <summary>
    /// Gets the volume family: <c>ml</c>, <c>cl</c>, <c>dl</c>, <c>l</c> and <c>us_fl_oz</c> (US fluid ounce).
    /// The reference unit is the milliliter.
    /// </summary>
    public static MeasurementFamily Volume { get; } = Define(
        "volume",
        ("ml", 1m),
        ("cl", 10m),
        ("dl", 100m),
        ("l", 1000m),
        ("us_fl_oz", 29.5735295625m));

    /// <summary>
    /// Gets every built-in family.
    /// </summary>
    public static ReadOnlyCollection<MeasurementFamily> All { get; } = new[] { Weight, Length, Volume }.AsReadOnly();

    /// <summary>
    /// Finds a built-in family by code.
    /// </summary>
    /// <param name="code">The family's code.</param>
    /// <returns>The family, or <see langword="null"/> if no built-in family has this code.</returns>
    public static MeasurementFamily? Find(MeasurementFamilyCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return All.FirstOrDefault(family => family.Code == code);
    }

    private static MeasurementFamily Define(string code, params (string Code, decimal Factor)[] units) =>
        MeasurementFamily
            .Create(
                MeasurementFamilyCode.Parse(code),
                units.Select(unit => new MeasurementUnit(UnitCode.Parse(unit.Code), unit.Factor)))
            .Value;
}
