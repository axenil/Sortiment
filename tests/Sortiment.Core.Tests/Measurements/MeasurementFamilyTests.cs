using System.Globalization;
using Sortiment.Core.Measurements;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;

namespace Sortiment.Core.Tests.Measurements;

public sealed class MeasurementFamilyTests
{
    private static readonly MeasurementFamilyCode Speed = MeasurementFamilyCode.Parse("speed");

    [Fact]
    public void CreateReturnsAFamilyWithItsUnitsInOrder()
    {
        var family = MeasurementFamily.Create(Speed, [Unit("m_s", 1m), Unit("km_h", 0.2777777777777777777777777778m)])
            .Value;

        family.Code.ShouldBe(Speed);
        family.Units.Select(unit => unit.Code.Value).ShouldBe(["m_s", "km_h"]);
    }

    [Fact]
    public void CreateRequiresAtLeastOneUnit()
    {
        var error = MeasurementFamily.Create(Speed, []).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("measurement_family.unit_required");
        error.Context[ErrorContextKeys.MeasurementFamily].ShouldBe("speed");
    }

    [Fact]
    public void CreateReportsDuplicateUnitsAndInvalidFactors()
    {
        var result = MeasurementFamily.Create(Speed, [Unit("m_s", 1m), Unit("m_s", 2m), Unit("knot", 0m)]);

        result.Errors.Select(error => (error.Code, error.Context[ErrorContextKeys.Unit])).ShouldBe(
        [
            ("measurement_family.duplicate_unit", "m_s"),
            ("measurement_family.invalid_conversion_factor", "knot"),
        ]);
    }

    [Fact]
    public void CreateReportsDuplicateUnitIds()
    {
        var id = MeasurementUnitId.New();

        var error = MeasurementFamily.Create(
                Speed,
                [new MeasurementUnit(id, UnitCode.Parse("m_s"), 1m), new MeasurementUnit(id, UnitCode.Parse("knot"), 0.5m)])
            .Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("measurement_family.duplicate_unit_id");
        error.Context[ErrorContextKeys.UnitId].ShouldBe(id.ToString());
    }

    [Fact]
    public void RestoreKeepsTheIdAndUnits()
    {
        var id = MeasurementFamilyId.New();
        MeasurementUnit[] units = [Unit("m_s", 1m), Unit("knot", 0.514444m)];

        var family = MeasurementFamily.Restore(id, Speed, units);

        family.Id.ShouldBe(id);
        family.Code.ShouldBe(Speed);
        family.Units.ShouldBe(units);
    }

    [Fact]
    public void RestoreRejectsAFamilyWithoutUnits()
    {
        Should.Throw<ArgumentException>(() => MeasurementFamily.Restore(MeasurementFamilyId.New(), Speed, []));
    }

    [Fact]
    public void FindUnitReturnsTheUnitOrNull()
    {
        var kilogram = MeasurementFamilies.Weight.FindUnit(UnitCode.Parse("kg"))!;

        kilogram.ConversionFactor.ShouldBe(1000m);
        MeasurementFamilies.Weight.FindUnit(kilogram.Id).ShouldBeSameAs(kilogram);
        MeasurementFamilies.Weight.FindUnit(UnitCode.Parse("cm")).ShouldBeNull();
        MeasurementFamilies.Weight.FindUnit(MeasurementFamilies.Length.UnitId("cm")).ShouldBeNull();
    }

    [Theory]
    [InlineData("weight", "1", "kg", "g", "1000")]
    [InlineData("weight", "1", "lb", "g", "453.59237")]
    [InlineData("weight", "16", "oz", "lb", "1")]
    [InlineData("weight", "2500", "mg", "g", "2.5")]
    [InlineData("length", "12", "in", "ft", "1")]
    [InlineData("length", "1", "in", "cm", "2.54")]
    [InlineData("length", "1.5", "m", "mm", "1500")]
    [InlineData("volume", "1", "l", "dl", "10")]
    [InlineData("volume", "33", "cl", "ml", "330")]
    [InlineData("volume", "1", "us_fl_oz", "ml", "29.5735295625")]
    public void ConvertUsesExactFactors(string familyCode, string amount, string from, string to, string expected)
    {
        var family = MeasurementFamilies.Find(MeasurementFamilyCode.Parse(familyCode))!;

        var result = family.Convert(new MeasurementValue(Decimal(amount), family.UnitId(from)), family.UnitId(to));

        result.Value.ShouldBe(new MeasurementValue(Decimal(expected), family.UnitId(to)));
    }

    [Fact]
    public void ConvertToTheSameUnitKeepsTheAmountExactly()
    {
        var ounce = MeasurementFamilies.Weight.UnitId("oz");
        var value = new MeasurementValue(1.2345m, ounce);

        MeasurementFamilies.Weight.Convert(value, ounce).Value.ShouldBe(value);
    }

    [Fact]
    public void ConvertReportsEveryUnknownUnit()
    {
        var centimeter = MeasurementFamilies.Length.UnitId("cm");
        var milliliter = MeasurementFamilies.Volume.UnitId("ml");

        var result = MeasurementFamilies.Weight.Convert(new MeasurementValue(1m, centimeter), milliliter);

        result.Errors.Select(error => error.Context[ErrorContextKeys.UnitId])
            .ShouldBe([centimeter.ToString(), milliliter.ToString()]);
        result.Errors.ShouldAllBe(error => error.Code == "measurement_family.unknown_unit");
    }

    [Fact]
    public void BuiltInFamiliesCanBeFoundByCodeAndId()
    {
        MeasurementFamilies.All.Select(family => family.Code.Value).ShouldBe(["weight", "length", "volume"]);
        MeasurementFamilies.Find(MeasurementFamilyCode.Parse("length")).ShouldBeSameAs(MeasurementFamilies.Length);
        MeasurementFamilies.Find(MeasurementFamilyCode.Parse("temperature")).ShouldBeNull();
        MeasurementFamilies.Find(MeasurementFamilies.Volume.Id).ShouldBeSameAs(MeasurementFamilies.Volume);
        MeasurementFamilies.Find(MeasurementFamilyId.New()).ShouldBeNull();
    }

    [Fact]
    public void BuiltInIdsNeverChange()
    {
        var ids = MeasurementFamilies.All.SelectMany(family => family.Units
            .Select(unit => $"{family.Code}.{unit.Code} {unit.Id}")
            .Prepend($"{family.Code} {family.Id}"));

        ids.ShouldBe(
        [
            "weight 01a19c76-23e9-7b00-b282-abab6c5eb1f6",
            "weight.mg 01a19c76-23fd-7622-baeb-bf58e2358999",
            "weight.g 01a19c76-240d-73c0-8856-09ef36702e4f",
            "weight.kg 01a19c76-241d-720c-9146-6005bcc66f34",
            "weight.oz 01a19c76-242f-7620-8538-00513175d6ec",
            "weight.lb 01a19c76-2431-7bb9-81a3-e3c9557bc07b",
            "length 01a19c76-2440-707a-8a1c-56e45bfddc57",
            "length.mm 01a19c76-2451-7554-b4c2-e1a02cdc5d01",
            "length.cm 01a19c76-2460-7cce-8297-850354004388",
            "length.m 01a19c76-2471-7cb2-bab1-31704bc35442",
            "length.in 01a19c76-2482-7d1d-a6c1-3fbc2495d5e2",
            "length.ft 01a19c76-2494-7d7f-aa5e-d1a74f4a5b64",
            "volume 01a19c76-24a3-7e40-97e4-9992cae72b7a",
            "volume.ml 01a19c76-24b4-7340-a02b-d6d5e5871965",
            "volume.cl 01a19c76-24c5-70b8-9969-01d4871785d7",
            "volume.dl 01a19c76-24d5-7001-8b68-e090a5f61383",
            "volume.l 01a19c76-24e7-7012-b3b6-2fc876766b11",
            "volume.us_fl_oz 01a19c76-24f6-76fb-b7bf-d8c40d7593ad",
        ]);
    }

    [Fact]
    public void MeasurementValuesAreComparedExactlyAsGiven()
    {
        var kilogram = MeasurementFamilies.Weight.UnitId("kg");
        var gram = MeasurementFamilies.Weight.UnitId("g");

        new MeasurementValue(1m, kilogram).ShouldBe(new MeasurementValue(1m, kilogram));
        new MeasurementValue(1m, kilogram).ShouldNotBe(new MeasurementValue(1000m, gram));
    }

    private static MeasurementUnit Unit(string code, decimal factor) =>
        new(MeasurementUnitId.New(), UnitCode.Parse(code), factor);

    private static decimal Decimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
