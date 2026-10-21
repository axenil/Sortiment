namespace Sortiment.Core.Assets;

/// <summary>
/// The immutable, human-readable key of an asset family, for example <c>packshots</c> or <c>user_manuals</c>.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>Packshots</c> is rejected rather than lowercased.
/// </remarks>
public sealed record AssetFamilyCode
{
    private AssetFamilyCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates an asset family code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<AssetFamilyCode> Create(string? value) =>
        CodeRules.Validate(value, "Asset family code").Map(valid => new AssetFamilyCode(valid));

    /// <summary>
    /// Creates an asset family code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static AssetFamilyCode Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
