using System.Collections.Immutable;

namespace Sortiment.Core;

/// <summary>
/// Describes an expected failure, such as a validation or import error.
/// </summary>
/// <remarks>
/// <see cref="Code"/> is a stable, machine-readable identifier (for example <c>code.too_long</c>) that callers
/// can rely on. <see cref="Message"/> is a human-readable explanation and may change between versions.
/// </remarks>
public sealed class ResultError
{
    private readonly ImmutableDictionary<string, string> _context;

    private ResultError(string code, string message, ImmutableDictionary<string, string> context)
    {
        Code = code;
        Message = message;
        _context = context;
    }

    /// <summary>
    /// Creates an error without context.
    /// </summary>
    /// <param name="code">A stable, machine-readable error code, for example <c>code.too_long</c>.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or whitespace.</exception>
    public ResultError(string code, string message)
        : this(code, message, ImmutableDictionary<string, string>.Empty)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }

    /// <summary>
    /// Gets the stable, machine-readable error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable description of the error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets details about where the error occurred, such as the attribute, row or column.
    /// Keys are listed in <see cref="ErrorContextKeys"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Context => _context;

    /// <summary>
    /// Returns a copy of this error with a context entry added, or replaced if the key already exists.
    /// </summary>
    /// <param name="key">The context key, preferably one of <see cref="ErrorContextKeys"/>.</param>
    /// <param name="value">The context value.</param>
    /// <returns>A new error with the context entry set.</returns>
    public ResultError WithContext(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        return new ResultError(Code, Message, _context.SetItem(key, value));
    }

    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}
