namespace Sortiment.Core.Categories;

internal static class CategoryTreeErrors
{
    public static ResultError DuplicateCategory(CategoryTreeCode tree, CategoryCode category) =>
        Create(
            tree,
            category,
            "category_tree.duplicate_category",
            $"Category tree '{tree}' already has a category '{category}'.");

    public static ResultError CategoryNotFound(CategoryTreeCode tree, CategoryCode category) =>
        Create(
            tree,
            category,
            "category_tree.category_not_found",
            $"Category tree '{tree}' has no category '{category}'.");

    public static ResultError MoveIntoOwnSubtree(CategoryTreeCode tree, CategoryCode category, CategoryCode parent) =>
        Create(
            tree,
            category,
            "category_tree.move_into_own_subtree",
            $"Category '{category}' cannot be moved under '{parent}' in category tree '{tree}', because '{parent}' is the category itself or one of its subcategories.");

    public static ResultError InvalidPosition(CategoryTreeCode tree, CategoryCode category, int position, int maxPosition) =>
        Create(
                tree,
                category,
                "category_tree.invalid_position",
                $"Position {position} is not valid for category '{category}' in category tree '{tree}'. It must be between 0 and {maxPosition}.")
            .WithContext(ErrorContextKeys.Value, position.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static ResultError CategoryHasChildren(CategoryTreeCode tree, CategoryCode category, int childCount) =>
        Create(
            tree,
            category,
            "category_tree.category_has_children",
            $"Category '{category}' in category tree '{tree}' has {childCount} subcategories and cannot be removed. Move or remove them first.");

    private static ResultError Create(CategoryTreeCode tree, CategoryCode category, string code, string message) =>
        new ResultError(code, message)
            .WithContext(ErrorContextKeys.CategoryTree, tree.Value)
            .WithContext(ErrorContextKeys.Category, category.Value);
}
