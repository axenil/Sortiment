using System.Collections.ObjectModel;

namespace Sortiment.Core.Measurements;

/// <summary>
/// The measurement families that ship with Sortiment. Their codes are reserved.
/// </summary>
/// <remarks>
/// All conversion factors are exact: imperial and US units use their legal definitions in metric units.
/// The built-in families and units have fixed IDs that are the same in every installation and never change
/// (ADR 0013), so stored references to them stay valid across upgrades.
/// </remarks>
public static class MeasurementFamilies
{
    /// <summary>
    /// Gets the weight family: <c>mg</c>, <c>g</c>, <c>kg</c>, <c>oz</c> and <c>lb</c>. The reference unit is the gram.
    /// </summary>
    public static MeasurementFamily Weight { get; } = Define(
        "01a19c76-23e9-7b00-b282-abab6c5eb1f6",
        "weight",
        ("01a19c76-23fd-7622-baeb-bf58e2358999", "mg", 0.001m),
        ("01a19c76-240d-73c0-8856-09ef36702e4f", "g", 1m),
        ("01a19c76-241d-720c-9146-6005bcc66f34", "kg", 1000m),
        ("01a19c76-242f-7620-8538-00513175d6ec", "oz", 28.349523125m),
        ("01a19c76-2431-7bb9-81a3-e3c9557bc07b", "lb", 453.59237m));

    /// <summary>
    /// Gets the length family: <c>mm</c>, <c>cm</c>, <c>m</c>, <c>in</c> and <c>ft</c>. The reference unit is the
    /// millimeter.
    /// </summary>
    public static MeasurementFamily Length { get; } = Define(
        "01a19c76-2440-707a-8a1c-56e45bfddc57",
        "length",
        ("01a19c76-2451-7554-b4c2-e1a02cdc5d01", "mm", 1m),
        ("01a19c76-2460-7cce-8297-850354004388", "cm", 10m),
        ("01a19c76-2471-7cb2-bab1-31704bc35442", "m", 1000m),
        ("01a19c76-2482-7d1d-a6c1-3fbc2495d5e2", "in", 25.4m),
        ("01a19c76-2494-7d7f-aa5e-d1a74f4a5b64", "ft", 304.8m));

    /// <summary>
    /// Gets the volume family: <c>ml</c>, <c>cl</c>, <c>dl</c>, <c>l</c> and <c>us_fl_oz</c> (US fluid ounce).
    /// The reference unit is the milliliter.
    /// </summary>
    public static MeasurementFamily Volume { get; } = Define(
        "01a19c76-24a3-7e40-97e4-9992cae72b7a",
        "volume",
        ("01a19c76-24b4-7340-a02b-d6d5e5871965", "ml", 1m),
        ("01a19c76-24c5-70b8-9969-01d4871785d7", "cl", 10m),
        ("01a19c76-24d5-7001-8b68-e090a5f61383", "dl", 100m),
        ("01a19c76-24e7-7012-b3b6-2fc876766b11", "l", 1000m),
        ("01a19c76-24f6-76fb-b7bf-d8c40d7593ad", "us_fl_oz", 29.5735295625m));

    /// <summary>
    /// Gets every built-in family.
    /// </summary>
    public static ReadOnlyCollection<MeasurementFamily> All { get; } = new[] { Weight, Length, Volume }.AsReadOnly();

    /// <summary>
    /// Finds a built-in family by code, for example to resolve a family given in an import or an API request.
    /// </summary>
    /// <param name="code">The family's code.</param>
    /// <returns>The family, or <see langword="null"/> if no built-in family has this code.</returns>
    public static MeasurementFamily? Find(MeasurementFamilyCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return All.FirstOrDefault(family => family.Code == code);
    }

    /// <summary>
    /// Finds a built-in family by ID, for example to restore a stored measurement attribute.
    /// </summary>
    /// <param name="id">The family's ID.</param>
    /// <returns>The family, or <see langword="null"/> if no built-in family has this ID.</returns>
    public static MeasurementFamily? Find(MeasurementFamilyId id) => All.FirstOrDefault(family => family.Id == id);

    private static MeasurementFamily Define(
        string id,
        string code,
        params (string Id, string Code, decimal Factor)[] units) =>
        MeasurementFamily.Restore(
            new MeasurementFamilyId(Guid.Parse(id)),
            MeasurementFamilyCode.Parse(code),
            units.Select(unit => new MeasurementUnit(
                new MeasurementUnitId(Guid.Parse(unit.Id)),
                UnitCode.Parse(unit.Code),
                unit.Factor)));
}
