using Sortiment.Core.Attributes;

namespace Sortiment.Core.Families;

internal static class FamilyVariantErrors
{
    public static ResultError AxisRequired(FamilyVariantCode familyVariant) =>
        Create(
            familyVariant,
            "family_variant.axis_required",
            $"Family variant '{familyVariant}' must have at least one axis.");

    public static ResultError TooManyAxes(FamilyVariantCode familyVariant, int count) =>
        Create(
            familyVariant,
            "family_variant.too_many_axes",
            $"Family variant '{familyVariant}' has {count} axes. The maximum is {FamilyVariant.MaxAxes}.");

    public static ResultError DuplicateAxis(FamilyVariantCode familyVariant, AttributeCode attribute) =>
        Create(
                familyVariant,
                "family_variant.duplicate_axis",
                $"Family variant '{familyVariant}' has attribute '{attribute}' as an axis more than once.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError AttributeNotInFamily(
        FamilyVariantCode familyVariant,
        FamilyCode family,
        AttributeCode attribute) =>
        Create(
                familyVariant,
                "family_variant.attribute_not_in_family",
                $"Family variant '{familyVariant}' uses attribute '{attribute}', which is not in family '{family}'.")
            .WithContext(ErrorContextKeys.Family, family.Value)
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError InvalidAxisType(FamilyVariantCode familyVariant, AttributeDefinition attribute) =>
        Create(
                familyVariant,
                "family_variant.invalid_axis_type",
                $"Attribute '{attribute.Code}' is of type {attribute.Type} and cannot be an axis of family variant '{familyVariant}'. Axes must be single-select or boolean attributes.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Code.Value);

    public static ResultError AxisNotGlobal(FamilyVariantCode familyVariant, AttributeCode attribute) =>
        Create(
                familyVariant,
                "family_variant.axis_not_global",
                $"Attribute '{attribute}' is localizable or scopable and cannot be an axis of family variant '{familyVariant}'.")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError AxisCannotBeRemoved(FamilyVariantCode familyVariant, AttributeId attribute) =>
        Create(
                familyVariant,
                "family_variant.axis_cannot_be_removed",
                $"The attribute with ID {attribute} is an axis of family variant '{familyVariant}' and cannot stop being a variant attribute.")
            .WithContext(ErrorContextKeys.AttributeId, attribute.ToString());

    private static ResultError Create(FamilyVariantCode familyVariant, string code, string message) =>
        new ResultError(code, message).WithContext(ErrorContextKeys.FamilyVariant, familyVariant.Value);
}
