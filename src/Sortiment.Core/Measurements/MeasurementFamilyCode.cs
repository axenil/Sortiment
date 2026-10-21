namespace Sortiment.Core.Measurements;

/// <summary>
/// The immutable, human-readable key of a measurement family, for example <c>weight</c> or <c>length</c>.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>Weight</c> is rejected rather than lowercased.
/// </remarks>
public sealed record MeasurementFamilyCode
{
    private MeasurementFamilyCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a measurement family code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<MeasurementFamilyCode> Create(string? value) =>
        CodeRules.Validate(value, "Measurement family code").Map(valid => new MeasurementFamilyCode(valid));

    /// <summary>
    /// Creates a measurement family code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static MeasurementFamilyCode Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
