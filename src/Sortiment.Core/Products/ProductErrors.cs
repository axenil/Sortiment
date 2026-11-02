using Sortiment.Core.Attributes;
using Sortiment.Core.Families;

namespace Sortiment.Core.Products;

internal static class ProductErrors
{
    public static ResultError AttributeNotInFamily(Product product, Family family, AttributeCode attribute) =>
        NotInFamily(product, family, $"'{attribute}'")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError AttributeNotInFamily(Product product, Family family, AttributeId attribute) =>
        NotInFamily(product, family, $"with ID {attribute}")
            .WithContext(ErrorContextKeys.AttributeId, attribute.ToString());

    public static ResultError AttributeOnModel(Product product, FamilyVariant familyVariant, AttributeCode attribute) =>
        OnModel(product, familyVariant, $"'{attribute}'")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError AttributeOnModel(Product product, FamilyVariant familyVariant, AttributeId attribute) =>
        OnModel(product, familyVariant, $"with ID {attribute}")
            .WithContext(ErrorContextKeys.AttributeId, attribute.ToString());

    public static ResultError AxisValueMissing(Product product, FamilyVariant familyVariant, AttributeCode axis) =>
        new ResultError(
                "product.axis_value_missing",
                $"Variant '{product.Sku}' has no value for axis '{axis}' of family variant '{familyVariant.Code}'.")
            .WithContext(ErrorContextKeys.Product, product.Sku.Value)
            .WithContext(ErrorContextKeys.FamilyVariant, familyVariant.Code.Value)
            .WithContext(ErrorContextKeys.Attribute, axis.Value);

    private static ResultError OnModel(Product product, FamilyVariant familyVariant, string attributeDescription) =>
        new ResultError(
                "product.attribute_on_model",
                $"The attribute {attributeDescription} is shared by the variants of family variant '{familyVariant.Code}', so its value belongs on the product model, not on variant '{product.Sku}'.")
            .WithContext(ErrorContextKeys.Product, product.Sku.Value)
            .WithContext(ErrorContextKeys.FamilyVariant, familyVariant.Code.Value);

    private static ResultError NotInFamily(Product product, Family family, string attributeDescription) =>
        new ResultError(
                "product.attribute_not_in_family",
                $"Product '{product.Sku}' is in family '{family.Code}', which does not have the attribute {attributeDescription}.")
            .WithContext(ErrorContextKeys.Product, product.Sku.Value)
            .WithContext(ErrorContextKeys.Family, family.Code.Value);
}
