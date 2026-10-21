namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="Attributes.AttributeType.Text"/>, <see cref="Attributes.AttributeType.TextArea"/> or
/// <see cref="Attributes.AttributeType.Identifier"/> attribute.
/// </summary>
public sealed record TextValue : AttributeValue
{
    /// <summary>
    /// Creates a text value. The text is stored exactly as given.
    /// </summary>
    /// <param name="text">The text.</param>
    public TextValue(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    public string Text { get; }
}
