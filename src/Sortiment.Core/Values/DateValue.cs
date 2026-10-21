namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="Attributes.AttributeType.Date"/> attribute.
/// </summary>
/// <param name="Value">The date.</param>
public sealed record DateValue(DateOnly Value) : AttributeValue;
