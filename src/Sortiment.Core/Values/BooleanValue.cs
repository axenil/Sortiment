namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="Attributes.AttributeType.Boolean"/> attribute.
/// </summary>
/// <param name="Value">The yes/no value.</param>
public sealed record BooleanValue(bool Value) : AttributeValue;
