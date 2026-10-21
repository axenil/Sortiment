using System.Text.RegularExpressions;

namespace Sortiment.Core;

/// <summary>
/// A language, optionally narrowed by script and region, written as a BCP 47 tag such as <c>en</c>, <c>sv-SE</c>
/// or <c>zh-Hant-TW</c>.
/// </summary>
/// <remarks>
/// Tags must use canonical casing: a lowercase language, a title-case script and an uppercase region.
/// Input is never normalized, so <c>sv-se</c> and <c>sv_SE</c> are rejected.
/// Validation is purely syntactic and does not depend on the culture data installed on the machine.
/// </remarks>
public sealed partial record Locale
{
    private Locale(string code) => Code = code;

    /// <summary>
    /// Gets the BCP 47 tag.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Validates input and creates a locale. Use this for input that may be invalid.
    /// </summary>
    /// <param name="code">The candidate BCP 47 tag.</param>
    /// <returns>The locale, or the error <c>locale.empty</c> or <c>locale.invalid_format</c>.</returns>
    public static Result<Locale> Create(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Result.Failure<Locale>(new ResultError("locale.empty", "Locale cannot be empty."));
        }

        if (!TagPattern().IsMatch(code))
        {
            return Result.Failure<Locale>(new ResultError(
                    "locale.invalid_format",
                    $"Locale '{code}' must be a BCP 47 tag with canonical casing, such as 'en', 'sv-SE' or 'zh-Hant-TW'.")
                .WithContext(ErrorContextKeys.Value, code));
        }

        return Result.Success(new Locale(code));
    }

    /// <summary>
    /// Creates a locale from a tag that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="code">The BCP 47 tag.</param>
    /// <returns>The locale.</returns>
    /// <exception cref="ArgumentException"><paramref name="code"/> is not a valid tag.</exception>
    public static Locale Parse(string code) => CodeRules.ValueOrThrow(Create(code), nameof(code));

    /// <inheritdoc />
    public override string ToString() => Code;

    [GeneratedRegex("^[a-z]{2,3}(-[A-Z][a-z]{3})?(-([A-Z]{2}|[0-9]{3}))?$", RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();
}
