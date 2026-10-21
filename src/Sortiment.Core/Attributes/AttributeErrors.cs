using System.Globalization;

namespace Sortiment.Core.Attributes;

internal static class AttributeErrors
{
    public static ResultError IdentifierCannotBeLocalizable(AttributeCode attribute) =>
        new ResultError(
                "attribute.identifier_cannot_be_localizable",
                $"Attribute '{attribute}' is an identifier, so it cannot be localizable.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError IdentifierCannotBeScopable(AttributeCode attribute) =>
        new ResultError(
                "attribute.identifier_cannot_be_scopable",
                $"Attribute '{attribute}' is an identifier, so it cannot be scopable.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError OptionsNotSupported(AttributeCode attribute, AttributeType type) =>
        new ResultError(
                "attribute.options_not_supported",
                $"Attribute '{attribute}' has type {type}. Only select attributes have options.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError OptionAlreadyExists(AttributeCode attribute, AttributeOptionCode option) =>
        new ResultError(
                "attribute.option_already_exists",
                $"Attribute '{attribute}' already has an option '{option}'.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value)
            .WithContext(ErrorContextKeys.Option, option.Value);

    public static ResultError ConstraintNotSupported(AttributeCode attribute, AttributeType type, string constraint) =>
        new ResultError(
                "attribute.constraint_not_supported",
                $"Attribute '{attribute}' has type {type}, which does not support {constraint}.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError InvalidMaxLength(AttributeCode attribute, int maxLength) =>
        new ResultError(
                "attribute.invalid_max_length",
                $"The maximum length of attribute '{attribute}' must be at least 1, but was {maxLength}.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError InvalidRange(AttributeCode attribute, decimal minimum, decimal maximum) =>
        new ResultError(
                "attribute.invalid_range",
                $"The minimum of attribute '{attribute}' ({minimum.ToString(CultureInfo.InvariantCulture)}) is greater than its maximum ({maximum.ToString(CultureInfo.InvariantCulture)}).")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError InvalidDecimalPlaces(AttributeCode attribute, int decimalPlaces) =>
        new ResultError(
                "attribute.invalid_decimal_places",
                $"The decimal places of attribute '{attribute}' must be between 0 and {AttributeDefinition.MaxDecimalPlaces}, but was {decimalPlaces}.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError InvalidMaxCount(AttributeCode attribute, int maxCount) =>
        new ResultError(
                "attribute.invalid_max_count",
                $"The maximum count of attribute '{attribute}' must be at least 1, but was {maxCount}.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError OptionNotFound(AttributeCode attribute, AttributeOptionCode option) =>
        new ResultError(
                "attribute.option_not_found",
                $"Attribute '{attribute}' has no option '{option}'.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value)
            .WithContext(ErrorContextKeys.Option, option.Value);
}
