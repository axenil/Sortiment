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

    private static ResultError NotInFamily(Product product, Family family, string attributeDescription) =>
        new ResultError(
                "product.attribute_not_in_family",
                $"Product '{product.Sku}' is in family '{family.Code}', which does not have the attribute {attributeDescription}.")
            .WithContext(ErrorContextKeys.Product, product.Sku.Value)
            .WithContext(ErrorContextKeys.Family, family.Code.Value);
}
