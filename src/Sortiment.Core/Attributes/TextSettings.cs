using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings of <see cref="AttributeType.Text"/>, <see cref="AttributeType.TextArea"/> and
/// <see cref="AttributeType.Identifier"/> attributes.
/// </summary>
/// <remarks>
/// Values must not be blank. <see cref="AttributeType.Text"/> and <see cref="AttributeType.Identifier"/> values must be
/// a single line, and identifiers cannot start or end with whitespace.
/// </remarks>
public sealed class TextSettings : AttributeSettings
{
    private readonly AttributeCode _attribute;

    internal TextSettings(AttributeCode attribute) => _attribute = attribute;

    /// <summary>
    /// Gets the maximum length of a value, counted in Unicode code points, or <see langword="null"/> for no limit.
    /// </summary>
    public int? MaxLength { get; private set; }

    /// <summary>
    /// Sets the maximum length of a value.
    /// </summary>
    /// <param name="maxLength">The maximum length in Unicode code points, or <see langword="null"/> for no limit.</param>
    /// <returns>Success, or the error <c>attribute.invalid_max_length</c> if the length is less than 1.</returns>
    public Result SetMaxLength(int? maxLength)
    {
        if (maxLength < 1)
        {
            return Result.Failure(AttributeErrors.InvalidMaxLength(_attribute, maxLength.Value));
        }

        MaxLength = maxLength;
        return Result.Success();
    }

    internal override IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value) =>
        value is TextValue text ? ValidateText(attribute, text.Text) : [ValueErrors.TypeMismatch(attribute, value)];

    private IEnumerable<ResultError> ValidateText(AttributeDefinition attribute, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return ValueErrors.Empty(attribute);
            yield break;
        }

        if (attribute.Type != AttributeType.TextArea && text.AsSpan().ContainsAny('\r', '\n'))
        {
            yield return ValueErrors.LineBreakNotAllowed(attribute);
        }

        if (attribute.Type == AttributeType.Identifier && text.Trim().Length != text.Length)
        {
            yield return ValueErrors.SurroundingWhitespace(attribute);
        }

        // Length is counted in Unicode code points, like PostgreSQL's varchar(n) and most channel specifications,
        // so an emoji counts as one character rather than two UTF-16 code units.
        var length = text.EnumerateRunes().Count();
        if (MaxLength is { } maxLength && length > maxLength)
        {
            yield return ValueErrors.TooLong(attribute, length, maxLength);
        }
    }
}
