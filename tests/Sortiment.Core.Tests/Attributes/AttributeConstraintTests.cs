using Sortiment.Core.Attributes;

namespace Sortiment.Core.Tests.Attributes;

public sealed class AttributeConstraintTests
{
    [Fact]
    public void NewAttributesHaveNoConstraints()
    {
        var attribute = Create(AttributeType.Number);

        attribute.MaxLength.ShouldBeNull();
        attribute.MinValue.ShouldBeNull();
        attribute.MaxValue.ShouldBeNull();
        attribute.DecimalPlaces.ShouldBeNull();
    }

    [Theory]
    [InlineData(AttributeType.Text)]
    [InlineData(AttributeType.TextArea)]
    [InlineData(AttributeType.Identifier)]
    public void TextualAttributesAcceptAMaxLength(AttributeType type)
    {
        var attribute = Create(type);

        attribute.SetMaxLength(150).IsSuccess.ShouldBeTrue();

        attribute.MaxLength.ShouldBe(150);
    }

    [Fact]
    public void MaxLengthCanBeRemoved()
    {
        var attribute = Create(AttributeType.Text);
        attribute.SetMaxLength(150);

        attribute.SetMaxLength(null).IsSuccess.ShouldBeTrue();

        attribute.MaxLength.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxLengthMustBePositive(int maxLength)
    {
        var attribute = Create(AttributeType.Text);

        attribute.SetMaxLength(maxLength).Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.invalid_max_length");
        attribute.MaxLength.ShouldBeNull();
    }

    [Theory]
    [InlineData(AttributeType.Number)]
    [InlineData(AttributeType.Boolean)]
    [InlineData(AttributeType.SingleSelect)]
    public void NonTextualAttributesRejectAMaxLength(AttributeType type)
    {
        Create(type).SetMaxLength(10).Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.constraint_not_supported");
    }

    [Fact]
    public void NumberAttributesAcceptARange()
    {
        var attribute = Create(AttributeType.Number);

        attribute.SetRange(0m, 100m).IsSuccess.ShouldBeTrue();

        attribute.MinValue.ShouldBe(0m);
        attribute.MaxValue.ShouldBe(100m);
    }

    [Fact]
    public void ARangeCanBeOpenOnEitherSide()
    {
        var attribute = Create(AttributeType.Number);

        attribute.SetRange(0m, null).IsSuccess.ShouldBeTrue();

        attribute.MinValue.ShouldBe(0m);
        attribute.MaxValue.ShouldBeNull();
    }

    [Fact]
    public void ARangeWithMinimumAboveMaximumIsRejected()
    {
        var attribute = Create(AttributeType.Number);

        attribute.SetRange(10m, 5m).Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.invalid_range");
        attribute.MinValue.ShouldBeNull();
    }

    [Fact]
    public void NonNumberAttributesRejectARange()
    {
        Create(AttributeType.Text).SetRange(0m, 1m).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("attribute.constraint_not_supported");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(AttributeDefinition.MaxDecimalPlaces)]
    public void NumberAttributesAcceptDecimalPlaces(int decimalPlaces)
    {
        var attribute = Create(AttributeType.Number);

        attribute.SetDecimalPlaces(decimalPlaces).IsSuccess.ShouldBeTrue();

        attribute.DecimalPlaces.ShouldBe(decimalPlaces);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(AttributeDefinition.MaxDecimalPlaces + 1)]
    public void DecimalPlacesOutsideTheSupportedRangeAreRejected(int decimalPlaces)
    {
        Create(AttributeType.Number).SetDecimalPlaces(decimalPlaces).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("attribute.invalid_decimal_places");
    }

    [Fact]
    public void NonNumberAttributesRejectDecimalPlaces()
    {
        Create(AttributeType.Date).SetDecimalPlaces(0).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("attribute.constraint_not_supported");
    }

    private static AttributeDefinition Create(AttributeType type) =>
        AttributeDefinition.Create(AttributeCode.Parse("attribute"), type).Value;
}
