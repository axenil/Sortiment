namespace Sortiment.Core.Attributes;

/// <summary>
/// The immutable, human-readable key of an option of a select attribute, for example <c>red</c>.
/// Option codes are unique within their attribute.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>Red</c> is rejected rather than lowercased.
/// </remarks>
public sealed record AttributeOptionCode
{
    private AttributeOptionCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates an option code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<AttributeOptionCode> Create(string? value) =>
        CodeRules.Validate(value, "Attribute option code").Map(valid => new AttributeOptionCode(valid));

    /// <summary>
    /// Creates an option code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static AttributeOptionCode Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
