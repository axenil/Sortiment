using Sortiment.Core.Attributes;
using Sortiment.Core.Categories;
using Sortiment.Core.Families;

namespace Sortiment.Core.Products;

internal static class ProductModelErrors
{
    public static ResultError AttributeNotInFamily(ProductModel model, Family family, AttributeCode attribute) =>
        NotInFamily(model, family, $"'{attribute}'")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError AttributeNotInFamily(ProductModel model, Family family, AttributeId attribute) =>
        NotInFamily(model, family, $"with ID {attribute}")
            .WithContext(ErrorContextKeys.AttributeId, attribute.ToString());

    public static ResultError AttributeNotOnModel(ProductModel model, FamilyVariant familyVariant, AttributeCode attribute) =>
        NotOnModel(model, familyVariant, $"'{attribute}'")
            .WithContext(ErrorContextKeys.Attribute, attribute.Value);

    public static ResultError AttributeNotOnModel(ProductModel model, FamilyVariant familyVariant, AttributeId attribute) =>
        NotOnModel(model, familyVariant, $"with ID {attribute}")
            .WithContext(ErrorContextKeys.AttributeId, attribute.ToString());

    public static ResultError CategoryNotFound(ProductModel model, CategoryTree tree, CategoryCode category) =>
        new ResultError(
                "product_model.category_not_found",
                $"Category tree '{tree.Code}' has no category '{category}', so product model '{model.Code}' cannot be put in it.")
            .WithContext(ErrorContextKeys.ProductModel, model.Code.Value)
            .WithContext(ErrorContextKeys.CategoryTree, tree.Code.Value)
            .WithContext(ErrorContextKeys.Category, category.Value);

    private static ResultError NotInFamily(ProductModel model, Family family, string attributeDescription) =>
        new ResultError(
                "product_model.attribute_not_in_family",
                $"Product model '{model.Code}' is in family '{family.Code}', which does not have the attribute {attributeDescription}.")
            .WithContext(ErrorContextKeys.ProductModel, model.Code.Value)
            .WithContext(ErrorContextKeys.Family, family.Code.Value);

    private static ResultError NotOnModel(ProductModel model, FamilyVariant familyVariant, string attributeDescription) =>
        new ResultError(
                "product_model.attribute_not_on_model",
                $"The attribute {attributeDescription} is set per variant in family variant '{familyVariant.Code}', so product model '{model.Code}' cannot have a value for it.")
            .WithContext(ErrorContextKeys.ProductModel, model.Code.Value)
            .WithContext(ErrorContextKeys.FamilyVariant, familyVariant.Code.Value);
}
