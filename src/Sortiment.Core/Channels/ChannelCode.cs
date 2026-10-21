namespace Sortiment.Core.Channels;

/// <summary>
/// The immutable, human-readable key of a channel, for example <c>ecommerce</c> or <c>google_shopping</c>.
/// Imports, feeds and the API refer to channels by code.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// Input is never normalized: <c>Ecommerce</c> is rejected rather than lowercased.
/// </remarks>
public sealed record ChannelCode
{
    private ChannelCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a channel code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<ChannelCode> Create(string? value) =>
        CodeRules.Validate(value, "Channel code").Map(valid => new ChannelCode(valid));

    /// <summary>
    /// Creates a channel code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static ChannelCode Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <inheritdoc />
    public override string ToString() => Value;
}
