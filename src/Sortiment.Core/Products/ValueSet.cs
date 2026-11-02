using Sortiment.Core.Attributes;
using Sortiment.Core.Values;

namespace Sortiment.Core.Products;

/// <summary>
/// The values of a product or product model: at most one value per attribute and scope (ADR 0006).
/// </summary>
internal sealed class ValueSet
{
    private readonly Dictionary<(AttributeId Attribute, ValueScope Scope), AttributeValue> _values = [];

    public IReadOnlyList<ProductValue> Values =>
        _values.Select(pair => new ProductValue(pair.Key.Attribute, pair.Key.Scope, pair.Value)).ToList().AsReadOnly();

    public AttributeValue? Get(AttributeId attribute, ValueScope scope) =>
        _values.GetValueOrDefault((attribute, scope));

    public bool Has(AttributeId attribute, ValueScope scope) => _values.ContainsKey((attribute, scope));

    /// <summary>
    /// Validates a value against its attribute and, if it is valid, stores it in place of any value in the same scope.
    /// </summary>
    /// <returns>Success, or the validation errors without any context of the owner.</returns>
    public Result SetIfValid(AttributeDefinition attribute, ValueScope scope, AttributeValue value)
    {
        var validation = attribute.Validate(value, scope);
        if (validation.IsSuccess)
        {
            _values[(attribute.Id, scope)] = value;
        }

        return validation;
    }

    /// <summary>
    /// Adds a stored value without validating it, for <c>Restore</c>.
    /// </summary>
    /// <returns><see langword="false"/> if there already is a value for the attribute in the same scope.</returns>
    public bool TryRestore(ProductValue value) => _values.TryAdd((value.Attribute, value.Scope), value.Value);

    public bool Remove(AttributeId attribute, ValueScope scope) => _values.Remove((attribute, scope));
}
