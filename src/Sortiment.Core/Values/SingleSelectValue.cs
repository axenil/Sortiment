using Sortiment.Core.Attributes;

namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="AttributeType.SingleSelect"/> attribute: one of the attribute's options.
/// </summary>
/// <param name="Option">The ID of the selected option.</param>
public sealed record SingleSelectValue(AttributeOptionId Option) : AttributeValue;
