using System.Text.RegularExpressions;

namespace Sortiment.Core.Files;

/// <summary>
/// The type of a file's content, written as <c>type/subtype</c>, for example <c>image/jpeg</c> or
/// <c>application/pdf</c> (RFC 6838).
/// </summary>
/// <remarks>
/// Media types are case-insensitive, so they are stored in lowercase. Parameters such as <c>; charset=utf-8</c> are
/// not part of a media type and are rejected.
/// </remarks>
public sealed partial record MediaType
{
    private MediaType(string type, string subtype)
    {
        Type = type;
        Subtype = subtype;
    }

    /// <summary>
    /// Gets the top-level type, for example <c>image</c>.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the subtype, for example <c>jpeg</c>.
    /// </summary>
    public string Subtype { get; }

    /// <summary>
    /// Validates input and creates a media type. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate media type.</param>
    /// <returns>The media type, or the error <c>media_type.empty</c> or <c>media_type.invalid_format</c>.</returns>
    public static Result<MediaType> Create(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Result.Failure<MediaType>(new ResultError("media_type.empty", "Media type cannot be empty."));
        }

        var parts = value.Split('/');
        if (parts.Length != 2 || !IsRestrictedName(parts[0]) || !IsRestrictedName(parts[1]))
        {
            return Result.Failure<MediaType>(new ResultError(
                    "media_type.invalid_format",
                    $"Media type '{value}' must have the form type/subtype, for example 'image/jpeg', without parameters.")
                .WithContext(ErrorContextKeys.Value, value));
        }

        return Result.Success(new MediaType(parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant()));
    }

    /// <summary>
    /// Creates a media type from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The media type.</param>
    /// <returns>The media type.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid media type.</exception>
    public static MediaType Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <inheritdoc />
    public override string ToString() => $"{Type}/{Subtype}";

    internal static bool IsRestrictedName(string name) => RestrictedName().IsMatch(name);

    // A restricted name as defined in RFC 6838, section 4.2.
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]{0,126}$", RegexOptions.CultureInvariant)]
    private static partial Regex RestrictedName();
}
