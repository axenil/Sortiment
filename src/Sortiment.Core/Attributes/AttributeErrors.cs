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

    public static ResultError OptionNotFound(AttributeCode attribute, AttributeOptionCode option) =>
        new ResultError(
                "attribute.option_not_found",
                $"Attribute '{attribute}' has no option '{option}'.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value)
            .WithContext(ErrorContextKeys.Option, option.Value);
}
