namespace Sortiment.Core.Categories;

/// <summary>
/// The immutable, human-readable key of a category tree, for example <c>web_shop</c>.
/// Imports, feeds and the API refer to category trees by code.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>Web_Shop</c> is rejected rather than lowercased.
/// </remarks>
public sealed record CategoryTreeCode
{
    private CategoryTreeCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a category tree code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<CategoryTreeCode> Create(string? value) =>
        CodeRules.Validate(value, "Category tree code").Map(valid => new CategoryTreeCode(valid));

    /// <summary>
    /// Creates a category tree code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static CategoryTreeCode Parse(string value) => Create(value).ValueOrThrow(nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
