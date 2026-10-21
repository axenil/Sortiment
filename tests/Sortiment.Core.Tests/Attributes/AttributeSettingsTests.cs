using Sortiment.Core.Attributes;

namespace Sortiment.Core.Tests.Attributes;

public sealed class AttributeSettingsTests
{
    [Fact]
    public void NewSettingsHaveNoConstraints()
    {
        Text().MaxLength.ShouldBeNull();

        var number = Number();
        number.MinValue.ShouldBeNull();
        number.MaxValue.ShouldBeNull();
        number.DecimalPlaces.ShouldBeNull();
    }

    [Fact]
    public void MaxLengthCanBeSetAndRemoved()
    {
        var settings = Text();

        settings.SetMaxLength(150).IsSuccess.ShouldBeTrue();
        settings.MaxLength.ShouldBe(150);

        settings.SetMaxLength(null).IsSuccess.ShouldBeTrue();
        settings.MaxLength.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxLengthMustBePositive(int maxLength)
    {
        var settings = Text();

        var error = settings.SetMaxLength(maxLength).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("attribute.invalid_max_length");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("attribute");
        settings.MaxLength.ShouldBeNull();
    }

    [Fact]
    public void ARangeCanBeSet()
    {
        var settings = Number();

        settings.SetRange(0m, 100m).IsSuccess.ShouldBeTrue();

        settings.MinValue.ShouldBe(0m);
        settings.MaxValue.ShouldBe(100m);
    }

    [Fact]
    public void ARangeCanBeOpenOnEitherSide()
    {
        var settings = Number();

        settings.SetRange(0m, null).IsSuccess.ShouldBeTrue();

        settings.MinValue.ShouldBe(0m);
        settings.MaxValue.ShouldBeNull();
    }

    [Fact]
    public void ARangeWithMinimumAboveMaximumIsRejected()
    {
        var settings = Number();

        settings.SetRange(10m, 5m).Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.invalid_range");
        settings.MinValue.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(NumberSettings.MaxDecimalPlaces)]
    public void DecimalPlacesCanBeSet(int decimalPlaces)
    {
        var settings = Number();

        settings.SetDecimalPlaces(decimalPlaces).IsSuccess.ShouldBeTrue();

        settings.DecimalPlaces.ShouldBe(decimalPlaces);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(NumberSettings.MaxDecimalPlaces + 1)]
    public void DecimalPlacesOutsideTheSupportedRangeAreRejected(int decimalPlaces)
    {
        Number().SetDecimalPlaces(decimalPlaces).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("attribute.invalid_decimal_places");
    }

    private static TextSettings Text() =>
        AttributeDefinition.Create(AttributeCode.Parse("attribute"), AttributeType.Text).Value
            .SettingsAs<TextSettings>();

    private static NumberSettings Number() =>
        AttributeDefinition.Create(AttributeCode.Parse("attribute"), AttributeType.Number).Value
            .SettingsAs<NumberSettings>();
}
