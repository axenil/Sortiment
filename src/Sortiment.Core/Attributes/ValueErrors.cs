using System.Globalization;
using Sortiment.Core.Assets;
using Sortiment.Core.Measurements;
using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

internal static class ValueErrors
{
    public static ResultError TypeMismatch(AttributeDefinition attribute, AttributeValue value) =>
        Create(
            attribute,
            "value.type_mismatch",
            $"Attribute '{attribute.Code}' has type {attribute.Type} and cannot hold a {value.GetType().Name}.");

    public static ResultError Empty(AttributeDefinition attribute) =>
        Create(
            attribute,
            "value.empty",
            $"The value of attribute '{attribute.Code}' is empty. Remove the value instead of setting it to empty.");

    public static ResultError LineBreakNotAllowed(AttributeDefinition attribute) =>
        Create(
            attribute,
            "value.line_break_not_allowed",
            $"Attribute '{attribute.Code}' has type {attribute.Type}, so its value must be a single line.");

    public static ResultError SurroundingWhitespace(AttributeDefinition attribute) =>
        Create(
            attribute,
            "value.surrounding_whitespace",
            $"The value of identifier attribute '{attribute.Code}' cannot start or end with whitespace.");

    public static ResultError TooLong(AttributeDefinition attribute, int length, int maxLength) =>
        Create(
            attribute,
            "value.too_long",
            $"The value of attribute '{attribute.Code}' is {length} characters long. The maximum is {maxLength}.");

    public static ResultError BelowMinimum(AttributeDefinition attribute, decimal value, decimal minimum) =>
        Create(
                attribute,
                "value.below_minimum",
                $"The value {Format(value)} of attribute '{attribute.Code}' is below the minimum of {Format(minimum)}.")
            .WithContext(ErrorContextKeys.Value, Format(value));

    public static ResultError AboveMaximum(AttributeDefinition attribute, decimal value, decimal maximum) =>
        Create(
                attribute,
                "value.above_maximum",
                $"The value {Format(value)} of attribute '{attribute.Code}' is above the maximum of {Format(maximum)}.")
            .WithContext(ErrorContextKeys.Value, Format(value));

    public static ResultError TooManyDecimals(AttributeDefinition attribute, decimal value, int decimalPlaces) =>
        Create(
                attribute,
                "value.too_many_decimals",
                $"The value {Format(value)} of attribute '{attribute.Code}' has more than {decimalPlaces} decimal places.")
            .WithContext(ErrorContextKeys.Value, Format(value));

    public static ResultError UnknownOption(AttributeDefinition attribute, AttributeOptionId option) =>
        Create(
                attribute,
                "value.unknown_option",
                $"Attribute '{attribute.Code}' has no option with ID {option}.")
            .WithContext(ErrorContextKeys.Value, option.ToString());

    public static ResultError DuplicateOption(
        AttributeDefinition attribute,
        AttributeOptionId option,
        AttributeOption? knownOption) =>
        Create(
                attribute,
                "value.duplicate_option",
                $"The value of attribute '{attribute.Code}' selects option {(knownOption is null ? option.ToString() : $"'{knownOption.Code}'")} more than once.")
            .WithContext(ErrorContextKeys.Value, option.ToString());

    public static ResultError UnknownUnit(AttributeDefinition attribute, MeasurementFamily family, UnitCode unit) =>
        Create(
                attribute,
                "value.unknown_unit",
                $"Attribute '{attribute.Code}' measures {family.Code}, which has no unit '{unit}'.")
            .WithContext(ErrorContextKeys.MeasurementFamily, family.Code.Value)
            .WithContext(ErrorContextKeys.Unit, unit.Value);

    public static ResultError DuplicateAsset(AttributeDefinition attribute, AssetId asset) =>
        Create(
                attribute,
                "value.duplicate_asset",
                $"The value of attribute '{attribute.Code}' contains asset {asset} more than once.")
            .WithContext(ErrorContextKeys.Value, asset.ToString());

    public static ResultError TooManyAssets(AttributeDefinition attribute, int count, int maxCount) =>
        Create(
            attribute,
            "value.too_many_assets",
            $"The value of attribute '{attribute.Code}' has {count} assets. The maximum is {maxCount}.");

    public static ResultError LocaleRequired(AttributeDefinition attribute) =>
        Create(
            attribute,
            "value.locale_required",
            $"Attribute '{attribute.Code}' is localizable, so its value needs a locale.");

    public static ResultError LocaleNotAllowed(AttributeDefinition attribute) =>
        Create(
            attribute,
            "value.locale_not_allowed",
            $"Attribute '{attribute.Code}' is not localizable, so its value cannot have a locale.");

    public static ResultError ChannelRequired(AttributeDefinition attribute) =>
        Create(
            attribute,
            "value.channel_required",
            $"Attribute '{attribute.Code}' is scopable, so its value needs a channel.");

    public static ResultError ChannelNotAllowed(AttributeDefinition attribute) =>
        Create(
            attribute,
            "value.channel_not_allowed",
            $"Attribute '{attribute.Code}' is not scopable, so its value cannot have a channel.");

    private static ResultError Create(AttributeDefinition attribute, string code, string message) =>
        new ResultError(code, message).WithContext(ErrorContextKeys.Attribute, attribute.Code.Value);

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
