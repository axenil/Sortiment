namespace Sortiment.Core.Categories;

/// <summary>
/// The immutable, human-readable key of a category, for example <c>linen_t_shirts</c>.
/// Imports, feeds and the API refer to categories by code, so codes are unique across all category trees.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>Linen_T_Shirts</c> is rejected rather than lowercased.
/// </remarks>
public sealed record CategoryCode
{
    private CategoryCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a category code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<CategoryCode> Create(string? value) =>
        CodeRules.Validate(value, "Category code").Map(valid => new CategoryCode(valid));

    /// <summary>
    /// Creates a category code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static CategoryCode Parse(string value) => Create(value).ValueOrThrow(nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
