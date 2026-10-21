using Sortiment.Core.Attributes;

namespace Sortiment.Core.Families;

internal static class FamilyErrors
{
    public static ResultError AttributeAlreadyInFamily(FamilyCode family, AttributeCode attribute) =>
        new ResultError(
                "family.attribute_already_in_family",
                $"Attribute '{attribute}' is already in family '{family}'.")
            .WithContext(ErrorContextKeys.Family, family.Value)
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError AttributeNotInFamily(FamilyCode family, string attribute) =>
        new ResultError(
                "family.attribute_not_in_family",
                $"Attribute '{attribute}' is not in family '{family}'.")
            .WithContext(ErrorContextKeys.Family, family.Value)
            .WithContext(ErrorContextKeys.Attribute, attribute);

    public static ResultError AttributeDefinitionMissing(FamilyCode family, AttributeId attribute) =>
        new ResultError(
                "family_schema.attribute_missing",
                $"Family '{family}' has attribute {attribute}, but no definition was given for it.")
            .WithContext(ErrorContextKeys.Family, family.Value)
            .WithContext(ErrorContextKeys.Attribute, attribute.ToString());
}
