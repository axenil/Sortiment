namespace Sortiment.Core.Families;

/// <summary>
/// The immutable, human-readable key of a family variant, for example <c>t_shirts_by_color_and_size</c>.
/// Imports, feeds and the API refer to family variants by code.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>T_Shirts</c> is rejected rather than lowercased.
/// </remarks>
public sealed record FamilyVariantCode
{
    private FamilyVariantCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a family variant code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<FamilyVariantCode> Create(string? value) =>
        CodeRules.Validate(value, "Family variant code").Map(valid => new FamilyVariantCode(valid));

    /// <summary>
    /// Creates a family variant code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static FamilyVariantCode Parse(string value) => Create(value).ValueOrThrow(nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
