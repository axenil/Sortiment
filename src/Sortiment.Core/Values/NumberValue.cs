namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="Attributes.AttributeType.Number"/> attribute.
/// </summary>
/// <param name="Value">The number.</param>
public sealed record NumberValue(decimal Value) : AttributeValue;
