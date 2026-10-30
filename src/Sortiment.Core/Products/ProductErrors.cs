using Sortiment.Core.Families;

namespace Sortiment.Core.Products;

internal static class ProductErrors
{
    public static ResultError AttributeNotInFamily(Product product, Family family, string attribute) =>
        new ResultError(
                "product.attribute_not_in_family",
                $"Product '{product.Sku}' is in family '{family.Code}', which does not have attribute '{attribute}'.")
            .WithContext(ErrorContextKeys.Product, product.Sku.Value)
            .WithContext(ErrorContextKeys.Family, family.Code.Value)
            .WithContext(ErrorContextKeys.Attribute, attribute);
}
