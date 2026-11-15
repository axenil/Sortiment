namespace Sortiment.Core.Categories;

/// <summary>
/// A hierarchy of categories that organizes products for presentation, for example <c>web_shop</c> or
/// <c>print_catalog</c> (ADR 0016). A catalog can have several trees, because channels organize the same products
/// differently.
/// </summary>
/// <remarks>
/// The tree holds all its categories and enforces the rules that span them: every category has one parent in the
/// tree or is at the top level, a category cannot be moved into its own subtree, and the order of each category's
/// children is kept. Category codes are unique within the tree; uniqueness across trees is enforced by whoever stores
/// them. The tree cannot see products, so callers that remove a category are responsible for removing it from the
/// products and product models in it.
/// </remarks>
public sealed class CategoryTree
{
    private readonly Dictionary<CategoryId, Category> _categoriesById = [];
    private readonly Dictionary<CategoryCode, Category> _categoriesByCode = [];
    private readonly Dictionary<CategoryId, List<CategoryId>> _children = [];
    private readonly List<CategoryId> _topLevel = [];

    private CategoryTree(CategoryTreeId id, CategoryTreeCode code)
    {
        Id = id;
        Code = code;
    }

    /// <summary>
    /// Gets the identity of the tree.
    /// </summary>
    public CategoryTreeId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the tree.
    /// </summary>
    public CategoryTreeCode Code { get; }

    /// <summary>
    /// Gets every category in the tree, depth first: each category is followed by its subcategories, and siblings
    /// appear in their order.
    /// </summary>
    public IReadOnlyList<Category> Categories
    {
        get
        {
            var categories = new List<Category>(_categoriesById.Count);
            AddDepthFirst(_topLevel, categories);
            return categories.AsReadOnly();
        }
    }

    /// <summary>
    /// Creates a new tree without categories.
    /// </summary>
    /// <param name="code">The tree's code.</param>
    /// <returns>The new tree.</returns>
    public static Result<CategoryTree> Create(CategoryTreeCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Result.Success(new CategoryTree(CategoryTreeId.New(), code));
    }

    /// <summary>
    /// Restores a tree from stored state (ADR 0011).
    /// </summary>
    /// <param name="id">The tree's ID.</param>
    /// <param name="code">The tree's code.</param>
    /// <param name="categories">
    /// The tree's categories. Siblings keep the order in which they are listed, so the order of
    /// <see cref="Categories"/> can be stored as it is.
    /// </param>
    /// <returns>The tree.</returns>
    /// <exception cref="ArgumentException">
    /// An ID or code is used more than once, a parent is not in the tree, or the parents form a cycle.
    /// </exception>
    public static CategoryTree Restore(CategoryTreeId id, CategoryTreeCode code, IEnumerable<Category> categories)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(categories);

        var tree = new CategoryTree(id, code);
        var problems = new RestoreProblems();
        var restored = new List<Category>();
        foreach (var category in categories)
        {
            ArgumentNullException.ThrowIfNull(category, nameof(categories));
            if (tree._categoriesById.ContainsKey(category.Id))
            {
                problems.Add($"Category ID {category.Id} is used more than once.");
            }
            else if (tree._categoriesByCode.ContainsKey(category.Code))
            {
                problems.Add($"Category code '{category.Code}' is used more than once.");
            }
            else
            {
                tree.Index(category);
                restored.Add(category);
            }
        }

        foreach (var category in restored)
        {
            if (category.ParentId is { } parentId && !tree._categoriesById.ContainsKey(parentId))
            {
                problems.Add($"The parent {parentId} of category '{category.Code}' is not in the tree.");
            }
            else if (tree.IsInCycle(category))
            {
                problems.Add($"Category '{category.Code}' is its own ancestor.");
            }
        }

        problems.ThrowIfAny($"Category tree '{code}'", nameof(categories));

        foreach (var category in restored)
        {
            tree.ChildList(category.ParentId).Add(category.Id);
        }

        return tree;
    }

    /// <summary>
    /// Finds a category in the tree by code.
    /// </summary>
    /// <param name="code">The category's code.</param>
    /// <returns>The category, or <see langword="null"/> if the tree has no category with that code.</returns>
    public Category? FindCategory(CategoryCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _categoriesByCode.GetValueOrDefault(code);
    }

    /// <summary>
    /// Finds a category in the tree by ID.
    /// </summary>
    /// <param name="id">The category's ID.</param>
    /// <returns>The category, or <see langword="null"/> if the tree has no category with that ID.</returns>
    public Category? FindCategory(CategoryId id) => _categoriesById.GetValueOrDefault(id);

    /// <summary>
    /// Gets the subcategories of a category, or the top-level categories, in their order.
    /// </summary>
    /// <param name="parentId">The parent category's ID, or <see langword="null"/> for the top level.</param>
    /// <returns>The children, in order.</returns>
    /// <exception cref="ArgumentException"><paramref name="parentId"/> is not a category in the tree.</exception>
    public IReadOnlyList<Category> Children(CategoryId? parentId)
    {
        if (parentId is { } id && !_categoriesById.ContainsKey(id))
        {
            throw new ArgumentException($"Category {id} is not in category tree '{Code}'.", nameof(parentId));
        }

        return ChildList(parentId).Select(child => _categoriesById[child]).ToList().AsReadOnly();
    }

    /// <summary>
    /// Adds a new category as the last child of a parent category, or as the last top-level category.
    /// </summary>
    /// <param name="code">The new category's code.</param>
    /// <param name="parent">The parent category's code, or <see langword="null"/> for the top level.</param>
    /// <returns>
    /// The new category, or the errors <c>category_tree.duplicate_category</c> and
    /// <c>category_tree.category_not_found</c> for an unknown parent.
    /// </returns>
    public Result<Category> AddCategory(CategoryCode code, CategoryCode? parent)
    {
        ArgumentNullException.ThrowIfNull(code);

        var errors = new List<ResultError>();
        if (_categoriesByCode.ContainsKey(code))
        {
            errors.Add(CategoryTreeErrors.DuplicateCategory(Code, code));
        }

        var parentCategory = parent is null ? null : FindCategory(parent);
        if (parent is not null && parentCategory is null)
        {
            errors.Add(CategoryTreeErrors.CategoryNotFound(Code, parent));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<Category>(errors);
        }

        var category = new Category(CategoryId.New(), code, parentCategory?.Id);
        Index(category);
        ChildList(category.ParentId).Add(category.Id);
        return Result.Success(category);
    }

    /// <summary>
    /// Moves a category, with its subcategories, to a position among the children of a parent category or among the
    /// top-level categories. Moving a category to a new position under its current parent reorders it among its
    /// siblings. The category keeps its ID, so products in it stay in it.
    /// </summary>
    /// <param name="category">The code of the category to move.</param>
    /// <param name="parent">The new parent category's code, or <see langword="null"/> for the top level.</param>
    /// <param name="position">
    /// The zero-based position among the new siblings, from 0 to the number of siblings without the moved category.
    /// </param>
    /// <returns>
    /// Success, or the errors <c>category_tree.category_not_found</c>,
    /// <c>category_tree.move_into_own_subtree</c> and <c>category_tree.invalid_position</c>.
    /// </returns>
    public Result MoveCategory(CategoryCode category, CategoryCode? parent, int position)
    {
        ArgumentNullException.ThrowIfNull(category);

        var errors = new List<ResultError>();
        var moved = FindCategory(category);
        if (moved is null)
        {
            errors.Add(CategoryTreeErrors.CategoryNotFound(Code, category));
        }

        var newParent = parent is null ? null : FindCategory(parent);
        if (parent is not null && newParent is null)
        {
            errors.Add(CategoryTreeErrors.CategoryNotFound(Code, parent));
        }

        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        if (newParent is not null && IsSelfOrDescendant(newParent.Id, moved!.Id))
        {
            return Result.Failure(CategoryTreeErrors.MoveIntoOwnSubtree(Code, category, newParent.Code));
        }

        var siblings = ChildList(newParent?.Id);
        var maxPosition = siblings.Count - (siblings.Contains(moved!.Id) ? 1 : 0);
        if (position < 0 || position > maxPosition)
        {
            return Result.Failure(CategoryTreeErrors.InvalidPosition(Code, category, position, maxPosition));
        }

        ChildList(moved.ParentId).Remove(moved.Id);
        siblings.Insert(position, moved.Id);
        Index(new Category(moved.Id, moved.Code, newParent?.Id));
        return Result.Success();
    }

    /// <summary>
    /// Removes a category that has no subcategories. The caller is responsible for removing the category from the
    /// products and product models in it.
    /// </summary>
    /// <param name="code">The category's code.</param>
    /// <returns>
    /// The removed category, or the errors <c>category_tree.category_not_found</c> and
    /// <c>category_tree.category_has_children</c>.
    /// </returns>
    public Result<Category> RemoveCategory(CategoryCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var category = FindCategory(code);
        if (category is null)
        {
            return Result.Failure<Category>(CategoryTreeErrors.CategoryNotFound(Code, code));
        }

        var childCount = ChildList(category.Id).Count;
        if (childCount > 0)
        {
            return Result.Failure<Category>(CategoryTreeErrors.CategoryHasChildren(Code, code, childCount));
        }

        ChildList(category.ParentId).Remove(category.Id);
        _children.Remove(category.Id);
        _categoriesById.Remove(category.Id);
        _categoriesByCode.Remove(category.Code);
        return Result.Success(category);
    }

    private void Index(Category category)
    {
        _categoriesById[category.Id] = category;
        _categoriesByCode[category.Code] = category;
    }

    private List<CategoryId> ChildList(CategoryId? parentId)
    {
        if (parentId is not { } id)
        {
            return _topLevel;
        }

        if (!_children.TryGetValue(id, out var children))
        {
            children = [];
            _children[id] = children;
        }

        return children;
    }

    private bool IsSelfOrDescendant(CategoryId candidate, CategoryId ancestor)
    {
        for (CategoryId? current = candidate; current is { } id; current = _categoriesById[id].ParentId)
        {
            if (id == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    // Only categories on the cycle itself are reported, not the categories below it, which would otherwise repeat
    // the same problem.
    private bool IsInCycle(Category category)
    {
        var visited = new HashSet<CategoryId>();
        for (var current = category.ParentId;
             current is { } id && _categoriesById.TryGetValue(id, out var parent);
             current = parent.ParentId)
        {
            if (id == category.Id)
            {
                return true;
            }

            if (!visited.Add(id))
            {
                return false;
            }
        }

        return false;
    }

    private void AddDepthFirst(List<CategoryId> ids, List<Category> categories)
    {
        foreach (var id in ids)
        {
            categories.Add(_categoriesById[id]);
            if (_children.TryGetValue(id, out var children))
            {
                AddDepthFirst(children, categories);
            }
        }
    }
}
