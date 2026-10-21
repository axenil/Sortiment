using System.Globalization;
using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Values;

namespace Sortiment.Core.Tests.Attributes;

public sealed class AttributeValueValidationTests
{
    private static readonly Locale Swedish = Locale.Parse("sv-SE");
    private static readonly ChannelId Web = ChannelId.New();

    public static TheoryData<AttributeType, AttributeValue> MatchingValues => new()
    {
        { AttributeType.Text, new TextValue("Linen shirt") },
        { AttributeType.TextArea, new TextValue("Line one\nLine two") },
        { AttributeType.Identifier, new TextValue("07350053850019") },
        { AttributeType.Boolean, new BooleanValue(false) },
        { AttributeType.Number, new NumberValue(-12.5m) },
        { AttributeType.Date, new DateValue(new DateOnly(2026, 10, 21)) },
    };

    public static TheoryData<AttributeType, AttributeValue> MismatchedValues => new()
    {
        { AttributeType.Text, new NumberValue(1m) },
        { AttributeType.Number, new TextValue("1") },
        { AttributeType.Boolean, new TextValue("true") },
        { AttributeType.Date, new TextValue("2026-10-21") },
        { AttributeType.SingleSelect, new MultiSelectValue([AttributeOptionId.New()]) },
        { AttributeType.MultiSelect, new SingleSelectValue(AttributeOptionId.New()) },
    };

    [Theory]
    [MemberData(nameof(MatchingValues))]
    public void AValueOfTheMatchingTypeIsValid(AttributeType type, AttributeValue value)
    {
        Create(type).Validate(value, ValueScope.Global).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(MismatchedValues))]
    public void AValueOfAnotherTypeIsRejected(AttributeType type, AttributeValue value)
    {
        var error = Create(type).Validate(value, ValueScope.Global).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("value.type_mismatch");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("attribute");
    }

    [Theory]
    [InlineData(AttributeType.Text, "")]
    [InlineData(AttributeType.TextArea, "  \n ")]
    [InlineData(AttributeType.Identifier, " ")]
    public void EmptyTextIsRejected(AttributeType type, string text)
    {
        Create(type).Validate(new TextValue(text), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.empty");
    }

    [Theory]
    [InlineData(AttributeType.Text, "Line one\nLine two")]
    [InlineData(AttributeType.Text, "Line one\r\nLine two")]
    [InlineData(AttributeType.Identifier, "123\r456")]
    public void SingleLineAttributesRejectLineBreaks(AttributeType type, string text)
    {
        Create(type).Validate(new TextValue(text), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.line_break_not_allowed");
    }

    [Theory]
    [InlineData(" 07350053850019")]
    [InlineData("07350053850019 ")]
    public void IdentifiersRejectSurroundingWhitespace(string text)
    {
        Create(AttributeType.Identifier).Validate(new TextValue(text), ValueScope.Global).Errors.ShouldHaveSingleItem()
            .Code.ShouldBe("value.surrounding_whitespace");
    }

    [Fact]
    public void TextAllowsSurroundingWhitespace()
    {
        Create(AttributeType.Text).Validate(new TextValue(" Shirt "), ValueScope.Global).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void TextLongerThanTheMaxLengthIsRejected()
    {
        var attribute = Create(AttributeType.Text);
        attribute.SetMaxLength(5);

        attribute.Validate(new TextValue("12345"), ValueScope.Global).IsSuccess.ShouldBeTrue();
        attribute.Validate(new TextValue("123456"), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.too_long");
    }

    [Fact]
    public void LengthIsCountedInCodePoints()
    {
        var attribute = Create(AttributeType.Text);
        attribute.SetMaxLength(3);

        attribute.Validate(new TextValue("a😀b"), ValueScope.Global).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ANumberOutsideTheRangeIsRejected()
    {
        var attribute = Create(AttributeType.Number);
        attribute.SetRange(0m, 10m);

        attribute.Validate(new NumberValue(0m), ValueScope.Global).IsSuccess.ShouldBeTrue();
        attribute.Validate(new NumberValue(10m), ValueScope.Global).IsSuccess.ShouldBeTrue();
        attribute.Validate(new NumberValue(-0.1m), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.below_minimum");
        attribute.Validate(new NumberValue(10.1m), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.above_maximum");
    }

    [Theory]
    [InlineData("1.5", true)]
    [InlineData("1.50", true)]
    [InlineData("1.500000", true)]
    [InlineData("1.55", false)]
    public void DecimalPlacesAreCountedWithoutTrailingZeros(string number, bool valid)
    {
        var attribute = Create(AttributeType.Number);
        attribute.SetDecimalPlaces(1);

        var result = attribute.Validate(
            new NumberValue(decimal.Parse(number, CultureInfo.InvariantCulture)),
            ValueScope.Global);

        result.IsSuccess.ShouldBe(valid);
    }

    [Fact]
    public void ZeroDecimalPlacesAllowsWholeNumbersOnly()
    {
        var attribute = Create(AttributeType.Number);
        attribute.SetDecimalPlaces(0);

        attribute.Validate(new NumberValue(3m), ValueScope.Global).IsSuccess.ShouldBeTrue();
        var error = attribute.Validate(new NumberValue(3.5m), ValueScope.Global).Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("value.too_many_decimals");
        error.Context[ErrorContextKeys.Value].ShouldBe("3.5");
    }

    [Fact]
    public void ANumberCanBreakSeveralConstraintsAtOnce()
    {
        var attribute = Create(AttributeType.Number);
        attribute.SetRange(0m, 10m);
        attribute.SetDecimalPlaces(0);

        attribute.Validate(new NumberValue(10.5m), ValueScope.Global).Errors.Select(error => error.Code)
            .ShouldBe(["value.above_maximum", "value.too_many_decimals"]);
    }

    [Fact]
    public void ASingleSelectValueMustBeAnOptionOfTheAttribute()
    {
        var attribute = Create(AttributeType.SingleSelect);
        var red = attribute.AddOption(AttributeOptionCode.Parse("red")).Value;

        attribute.Validate(new SingleSelectValue(red.Id), ValueScope.Global).IsSuccess.ShouldBeTrue();
        attribute.Validate(new SingleSelectValue(AttributeOptionId.New()), ValueScope.Global).Errors
            .ShouldHaveSingleItem().Code.ShouldBe("value.unknown_option");
    }

    [Fact]
    public void AMultiSelectValueReportsEveryUnknownAndDuplicateOption()
    {
        var attribute = Create(AttributeType.MultiSelect);
        var red = attribute.AddOption(AttributeOptionCode.Parse("red")).Value;
        var blue = attribute.AddOption(AttributeOptionCode.Parse("blue")).Value;
        var unknown = AttributeOptionId.New();

        var result = attribute.Validate(
            new MultiSelectValue([red.Id, unknown, red.Id, blue.Id, red.Id]),
            ValueScope.Global);

        result.Errors.Select(error => error.Code).ShouldBe(["value.unknown_option", "value.duplicate_option"]);
        result.Errors[1].Message.ShouldContain("'red'");
    }

    [Fact]
    public void AnEmptyMultiSelectValueIsRejected()
    {
        Create(AttributeType.MultiSelect).Validate(new MultiSelectValue([]), ValueScope.Global).Errors
            .ShouldHaveSingleItem().Code.ShouldBe("value.empty");
    }

    [Fact]
    public void AGlobalAttributeAcceptsOnlyTheGlobalScope()
    {
        var attribute = Create(AttributeType.Text);

        attribute.ValidateScope(ValueScope.Global).IsSuccess.ShouldBeTrue();
        attribute.ValidateScope(ValueScope.For(Swedish, Web)).Errors.Select(error => error.Code)
            .ShouldBe(["value.locale_not_allowed", "value.channel_not_allowed"]);
    }

    [Fact]
    public void ALocalizableAndScopableAttributeNeedsBothLocaleAndChannel()
    {
        var attribute = Create(AttributeType.Text, isLocalizable: true, isScopable: true);

        attribute.ValidateScope(ValueScope.For(Swedish, Web)).IsSuccess.ShouldBeTrue();
        attribute.ValidateScope(ValueScope.Global).Errors.Select(error => error.Code)
            .ShouldBe(["value.locale_required", "value.channel_required"]);
    }

    [Fact]
    public void ALocalizableAttributeNeedsALocaleButNoChannel()
    {
        var attribute = Create(AttributeType.Text, isLocalizable: true);

        attribute.ValidateScope(ValueScope.ForLocale(Swedish)).IsSuccess.ShouldBeTrue();
        attribute.ValidateScope(ValueScope.ForChannel(Web)).Errors.Select(error => error.Code)
            .ShouldBe(["value.locale_required", "value.channel_not_allowed"]);
    }

    [Fact]
    public void ValidateReportsScopeAndContentErrorsTogetherWithTheScopeAsContext()
    {
        var attribute = Create(AttributeType.Text, isScopable: true);
        attribute.SetMaxLength(3);

        var result = attribute.Validate(new TextValue("Too long"), ValueScope.For(Swedish, Web));

        result.Errors.Select(error => error.Code).ShouldBe(["value.locale_not_allowed", "value.too_long"]);
        result.Errors.ShouldAllBe(error =>
            error.Context[ErrorContextKeys.Attribute] == "attribute"
            && error.Context[ErrorContextKeys.Locale] == "sv-SE"
            && error.Context[ErrorContextKeys.Channel] == Web.ToString());
    }

    private static AttributeDefinition Create(
        AttributeType type,
        bool isLocalizable = false,
        bool isScopable = false) =>
        AttributeDefinition.Create(AttributeCode.Parse("attribute"), type, isLocalizable, isScopable).Value;
}
