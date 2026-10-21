using System.Globalization;
using Sortiment.Core.Measurements;
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
    public void FindUnitReturnsTheUnitOrNull()
    {
        MeasurementFamilies.Weight.FindUnit(UnitCode.Parse("kg"))!.ConversionFactor.ShouldBe(1000m);
        MeasurementFamilies.Weight.FindUnit(UnitCode.Parse("cm")).ShouldBeNull();
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
    public void ConvertUsesExactFactors(string family, string amount, string from, string to, string expected)
    {
        var result = MeasurementFamilies.Find(MeasurementFamilyCode.Parse(family))!
            .Convert(new MeasurementValue(Decimal(amount), UnitCode.Parse(from)), UnitCode.Parse(to));

        result.Value.ShouldBe(new MeasurementValue(Decimal(expected), UnitCode.Parse(to)));
    }

    [Fact]
    public void ConvertToTheSameUnitKeepsTheAmountExactly()
    {
        var value = new MeasurementValue(1.2345m, UnitCode.Parse("oz"));

        MeasurementFamilies.Weight.Convert(value, UnitCode.Parse("oz")).Value.ShouldBe(value);
    }

    [Fact]
    public void ConvertReportsEveryUnknownUnit()
    {
        var result = MeasurementFamilies.Weight.Convert(
            new MeasurementValue(1m, UnitCode.Parse("cm")),
            UnitCode.Parse("ml"));

        result.Errors.Select(error => error.Context[ErrorContextKeys.Unit]).ShouldBe(["cm", "ml"]);
        result.Errors.ShouldAllBe(error => error.Code == "measurement_family.unknown_unit");
    }

    [Fact]
    public void BuiltInFamiliesCanBeFoundByCode()
    {
        MeasurementFamilies.All.Select(family => family.Code.Value).ShouldBe(["weight", "length", "volume"]);
        MeasurementFamilies.Find(MeasurementFamilyCode.Parse("length")).ShouldBeSameAs(MeasurementFamilies.Length);
        MeasurementFamilies.Find(MeasurementFamilyCode.Parse("temperature")).ShouldBeNull();
    }

    [Fact]
    public void MeasurementValuesAreComparedExactlyAsGiven()
    {
        new MeasurementValue(1m, UnitCode.Parse("kg")).ShouldBe(new MeasurementValue(1m, UnitCode.Parse("kg")));
        new MeasurementValue(1m, UnitCode.Parse("kg")).ShouldNotBe(new MeasurementValue(1000m, UnitCode.Parse("g")));
    }

    private static MeasurementUnit Unit(string code, decimal factor) => new(UnitCode.Parse(code), factor);

    private static decimal Decimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
