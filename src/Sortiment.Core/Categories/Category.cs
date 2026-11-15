namespace Sortiment.Core.Categories;

/// <summary>
/// A category in a <see cref="CategoryTree"/>, for example <c>linen_t_shirts</c> under <c>t_shirts</c> in a web shop
/// tree (ADR 0016).
/// </summary>
/// <remarks>
/// Categories are immutable: when a category is moved, its tree replaces it with a category that has the same ID and
/// code and a new parent. Categories are added and changed through their tree, which enforces the rules that span
/// them. The public constructor is for <see cref="CategoryTree.Restore"/>.
/// </remarks>
public sealed record Category
{
    /// <summary>
    /// Creates a category, for example from stored state passed to <see cref="CategoryTree.Restore"/>.
    /// </summary>
    /// <param name="id">The category's ID.</param>
    /// <param name="code">The category's code.</param>
    /// <param name="parentId">The ID of the parent category, or <see langword="null"/> for a top-level category.</param>
    public Category(CategoryId id, CategoryCode code, CategoryId? parentId)
    {
        ArgumentNullException.ThrowIfNull(code);

        Id = id;
        Code = code;
        ParentId = parentId;
    }

    /// <summary>
    /// Gets the identity of the category.
    /// </summary>
    public CategoryId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the category.
    /// </summary>
    public CategoryCode Code { get; }

    /// <summary>
    /// Gets the ID of the parent category, or <see langword="null"/> if the category is at the top level of its tree.
    /// </summary>
    public CategoryId? ParentId { get; }
}
