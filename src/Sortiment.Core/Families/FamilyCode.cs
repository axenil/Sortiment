namespace Sortiment.Core.Families;

/// <summary>
/// The immutable, human-readable key of a family, for example <c>t_shirts</c> or <c>televisions</c>.
/// Imports, feeds and the API refer to families by code.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>T_Shirts</c> is rejected rather than lowercased.
/// </remarks>
public sealed record FamilyCode
{
    private FamilyCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a family code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<FamilyCode> Create(string? value) =>
        CodeRules.Validate(value, "Family code").Map(valid => new FamilyCode(valid));

    /// <summary>
    /// Creates a family code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static FamilyCode Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
