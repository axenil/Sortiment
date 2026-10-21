namespace Sortiment.Core.Files;

/// <summary>
/// Matches media types: either one exact type such as <c>application/pdf</c>, or every subtype of a top-level type
/// such as <c>image/*</c>.
/// </summary>
public sealed record MediaTypePattern
{
    private const string Wildcard = "*";

    private MediaTypePattern(string type, string? subtype)
    {
        Type = type;
        Subtype = subtype;
    }

    /// <summary>
    /// Gets the top-level type the pattern matches, for example <c>image</c>.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the subtype the pattern matches, or <see langword="null"/> if it matches every subtype.
    /// </summary>
    public string? Subtype { get; }

    /// <summary>
    /// Validates input and creates a pattern. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate pattern, for example <c>image/*</c> or <c>application/pdf</c>.</param>
    /// <returns>
    /// The pattern, or the error <c>media_type_pattern.empty</c> or <c>media_type_pattern.invalid_format</c>.
    /// </returns>
    public static Result<MediaTypePattern> Create(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Result.Failure<MediaTypePattern>(
                new ResultError("media_type_pattern.empty", "Media type pattern cannot be empty."));
        }

        var parts = value.Split('/');
        var isValid = parts.Length == 2
            && MediaType.IsRestrictedName(parts[0])
            && (parts[1] == Wildcard || MediaType.IsRestrictedName(parts[1]));
        if (!isValid)
        {
            return Result.Failure<MediaTypePattern>(new ResultError(
                    "media_type_pattern.invalid_format",
                    $"Media type pattern '{value}' must be a media type such as 'application/pdf' or a wildcard such as 'image/*'.")
                .WithContext(ErrorContextKeys.Value, value));
        }

        var subtype = parts[1] == Wildcard ? null : parts[1].ToLowerInvariant();
        return Result.Success(new MediaTypePattern(parts[0].ToLowerInvariant(), subtype));
    }

    /// <summary>
    /// Creates a pattern from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The pattern.</param>
    /// <returns>The pattern.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid pattern.</exception>
    public static MediaTypePattern Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <summary>
    /// Gets a value indicating whether a media type matches the pattern.
    /// </summary>
    /// <param name="mediaType">The media type.</param>
    /// <returns><see langword="true"/> if the media type matches.</returns>
    public bool Matches(MediaType mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);

        return mediaType.Type == Type && (Subtype is null || mediaType.Subtype == Subtype);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Type}/{Subtype ?? Wildcard}";
}
