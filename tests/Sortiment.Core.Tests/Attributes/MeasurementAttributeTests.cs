using Sortiment.Core.Attributes;
using Sortiment.Core.Measurements;
using Sortiment.Core.Values;

namespace Sortiment.Core.Tests.Attributes;

public sealed class MeasurementAttributeTests
{
    private static readonly AttributeCode NetWeight = AttributeCode.Parse("net_weight");
    private static readonly AttributeCode Name = AttributeCode.Parse("name");

    [Fact]
    public void CreateMeasurementReturnsAMeasurementAttributeWithItsFamily()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.Type.ShouldBe(AttributeType.Measurement);
        attribute.MeasurementFamily.ShouldBeSameAs(MeasurementFamilies.Weight);
        attribute.IsLocalizable.ShouldBeFalse();
        attribute.IsScopable.ShouldBeFalse();
    }

    [Fact]
    public void CreateRefusesTheMeasurementTypeBecauseItNeedsAFamily()
    {
        Should.Throw<ArgumentException>(() => AttributeDefinition.Create(NetWeight, AttributeType.Measurement));
    }

    [Fact]
    public void OtherAttributeTypesHaveNoMeasurementFamily()
    {
        AttributeDefinition.Create(Name, AttributeType.Text).Value.MeasurementFamily.ShouldBeNull();
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("lb")]
    public void AMeasurementInAUnitOfTheFamilyIsValid(string unit)
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.Validate(new MeasurementValue(1.2m, UnitCode.Parse(unit)), ValueScope.Global).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AMeasurementInAUnitOfAnotherFamilyIsRejected()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        var error = attribute.Validate(new MeasurementValue(1.2m, UnitCode.Parse("cm")), ValueScope.Global).Errors
            .ShouldHaveSingleItem();

        error.Code.ShouldBe("value.unknown_unit");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("net_weight");
        error.Context[ErrorContextKeys.MeasurementFamily].ShouldBe("weight");
        error.Context[ErrorContextKeys.Unit].ShouldBe("cm");
    }

    [Fact]
    public void MeasurementAttributesRejectOtherValueTypes()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.Validate(new NumberValue(1.2m), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.type_mismatch");
    }

    [Fact]
    public void MeasurementAttributesSupportNoNumberConstraints()
    {
        var attribute = AttributeDefinition.CreateMeasurement(NetWeight, MeasurementFamilies.Weight).Value;

        attribute.SetRange(0m, 10m).Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.constraint_not_supported");
    }
}
