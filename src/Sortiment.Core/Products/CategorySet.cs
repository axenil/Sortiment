using Sortiment.Core.Categories;

namespace Sortiment.Core.Products;

/// <summary>
/// The categories of a product or product model: each category at most once, in the order they were added (ADR 0016).
/// </summary>
internal sealed class CategorySet
{
    private readonly List<CategoryId> _categories = [];

    public IReadOnlyList<CategoryId> Categories => _categories.AsReadOnly();

    /// <summary>
    /// Adds a category unless it is already in the set.
    /// </summary>
    public void Add(CategoryId category)
    {
        if (!_categories.Contains(category))
        {
            _categories.Add(category);
        }
    }

    public bool Remove(CategoryId category) => _categories.Remove(category);

    /// <summary>
    /// Adds stored categories, for <c>Restore</c>, and reports each category that is listed more than once.
    /// </summary>
    public void Restore(IEnumerable<CategoryId> categories, RestoreProblems problems)
    {
        foreach (var category in categories)
        {
            if (_categories.Contains(category))
            {
                problems.Add($"Category {category} is listed more than once.");
            }
            else
            {
                _categories.Add(category);
            }
        }
    }
}
