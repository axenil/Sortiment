using Sortiment.Core.Attributes;
using Sortiment.Core.Measurements;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;

namespace Sortiment.Core.Tests.Attributes;

public sealed class MeasurementAttributeTests
{
    private static readonly AttributeCode NetWeight = AttributeCode.Parse("net_weight");
    private static readonly MeasurementUnitId Centimeter = MeasurementFamilies.Length.UnitId("cm");

    [Fact]
    public void CreateMeasurementReturnsAMeasurementAttributeWithItsFamily()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.Type.ShouldBe(AttributeType.Measurement);
        attribute.SettingsAs<MeasurementSettings>().Family.ShouldBeSameAs(MeasurementFamilies.Weight);
        attribute.IsLocalizable.ShouldBeFalse();
        attribute.IsScopable.ShouldBeFalse();
    }

    [Fact]
    public void RestoreMeasurementKeepsTheIdAndFamily()
    {
        var id = AttributeId.New();

        var attribute = AttributeDefinition.RestoreMeasurement(
            id,
            NetWeight,
            MeasurementFamilies.Weight,
            isLocalizable: false,
            isScopable: true);

        attribute.Id.ShouldBe(id);
        attribute.Type.ShouldBe(AttributeType.Measurement);
        attribute.SettingsAs<MeasurementSettings>().Family.ShouldBeSameAs(MeasurementFamilies.Weight);
        attribute.IsScopable.ShouldBeTrue();
    }

    [Fact]
    public void CreateRefusesTheMeasurementTypeBecauseItNeedsAFamily()
    {
        Should.Throw<ArgumentException>(() => AttributeDefinition.Create(NetWeight, AttributeType.Measurement));
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("lb")]
    public void AMeasurementInAUnitOfTheFamilyIsValid(string unit)
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.Validate(new MeasurementValue(1.2m, MeasurementFamilies.Weight.UnitId(unit)), ValueScope.Global).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AMeasurementInAUnitOfAnotherFamilyIsRejected()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        var error = attribute.Validate(new MeasurementValue(1.2m, Centimeter), ValueScope.Global).Errors
            .ShouldHaveSingleItem();

        error.Code.ShouldBe("value.unknown_unit");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("net_weight");
        error.Context[ErrorContextKeys.MeasurementFamily].ShouldBe("weight");
        error.Context[ErrorContextKeys.UnitId].ShouldBe(Centimeter.ToString());
    }

    [Fact]
    public void MeasurementAttributesRejectOtherValueTypes()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.Validate(new NumberValue(1.2m), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.type_mismatch");
    }

    [Fact]
    public void MeasurementAttributesHaveMeasurementSettings()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.Settings.ShouldBeOfType<MeasurementSettings>();
    }
}
