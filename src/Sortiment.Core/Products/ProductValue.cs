using Sortiment.Core.Attributes;
using Sortiment.Core.Values;

namespace Sortiment.Core.Products;

/// <summary>
/// One of a product's values: the value of an attribute in a scope.
/// </summary>
/// <param name="Attribute">The attribute the value belongs to.</param>
/// <param name="Scope">The locale and channel the value applies to.</param>
/// <param name="Value">The value.</param>
public sealed record ProductValue(AttributeId Attribute, ValueScope Scope, AttributeValue Value);
