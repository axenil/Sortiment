using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Families;
using Sortiment.Core.Values;

namespace Sortiment.Core.Products;

/// <summary>
/// A product: a SKU, a family, and values for the family's attributes.
/// </summary>
/// <remarks>
/// Operations that need attribute definitions take the <see cref="FamilySchema"/> of the product's family, resolved by
/// the caller (ADR 0010). SKU uniqueness is enforced by whoever stores products.
/// </remarks>
public sealed class Product
{
    private readonly Dictionary<(AttributeId Attribute, ValueScope Scope), AttributeValue> _values = [];

    private Product(ProductId id, Sku sku, FamilyId familyId)
    {
        Id = id;
        Sku = sku;
        FamilyId = familyId;
        IsEnabled = true;
    }

    /// <summary>
    /// Gets the identity of the product.
    /// </summary>
    public ProductId Id { get; }

    /// <summary>
    /// Gets the SKU that people and other systems use to refer to the product.
    /// </summary>
    public Sku Sku { get; private set; }

    /// <summary>
    /// Gets the identity of the product's family.
    /// </summary>
    public FamilyId FamilyId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the product is enabled. Disabled products are kept but not published to channels.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the product's values.
    /// </summary>
    public IReadOnlyList<ProductValue> Values =>
        _values.Select(pair => new ProductValue(pair.Key.Attribute, pair.Key.Scope, pair.Value)).ToList().AsReadOnly();

    /// <summary>
    /// Creates a new, enabled product without values.
    /// </summary>
    /// <param name="sku">The product's SKU.</param>
    /// <param name="schema">The schema of the product's family.</param>
    /// <returns>The new product.</returns>
    public static Result<Product> Create(Sku sku, FamilySchema schema)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(schema);

        return Result.Success(new Product(ProductId.New(), sku, schema.Family.Id));
    }

    /// <summary>
    /// Changes the product's SKU, for example after an ERP migration or to correct its casing.
    /// </summary>
    /// <param name="sku">The new SKU.</param>
    public void ChangeSku(Sku sku)
    {
        ArgumentNullException.ThrowIfNull(sku);

        Sku = sku;
    }

    /// <summary>
    /// Enables the product.
    /// </summary>
    public void Enable() => IsEnabled = true;

    /// <summary>
    /// Disables the product.
    /// </summary>
    public void Disable() => IsEnabled = false;

    /// <summary>
    /// Gets the value of an attribute in a scope.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>The value, or <see langword="null"/> if the product has no value there.</returns>
    public AttributeValue? GetValue(AttributeId attribute, ValueScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _values.GetValueOrDefault((attribute, scope));
    }

    /// <summary>
    /// Sets the value of an attribute in a scope, replacing any value already there.
    /// </summary>
    /// <param name="schema">The schema of the product's family.</param>
    /// <param name="attribute">The attribute's code.</param>
    /// <param name="scope">The locale and channel the value applies to.</param>
    /// <param name="value">The value.</param>
    /// <returns>
    /// Success, or the error <c>product.attribute_not_in_family</c>, or the errors from
    /// <see cref="AttributeDefinition.Validate"/>. Every error's context includes the product's SKU.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="schema"/> is not the schema of the product's family.</exception>
    public Result SetValue(FamilySchema schema, AttributeCode attribute, ValueScope scope, AttributeValue value)
    {
        EnsureOwnFamily(schema);
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(value);

        var definition = schema.FindAttribute(attribute);
        if (definition is null)
        {
            return Result.Failure(ProductErrors.AttributeNotInFamily(this, schema.Family, attribute.Value));
        }

        var validation = definition.Validate(value, scope);
        if (validation.IsFailure)
        {
            return Result.Failure(validation.Errors.Select(AddProductContext));
        }

        _values[(definition.Id, scope)] = value;
        return Result.Success();
    }

    /// <summary>
    /// Removes the value of an attribute in a scope. This is the only way to clear a value.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <param name="scope">The scope.</param>
    /// <returns><see langword="true"/> if a value was removed; <see langword="false"/> if there was none.</returns>
    public bool RemoveValue(AttributeId attribute, ValueScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _values.Remove((attribute, scope));
    }

    /// <summary>
    /// Moves the product to another family. Values for attributes that the new family does not have are removed.
    /// </summary>
    /// <param name="schema">The schema of the new family.</param>
    /// <returns>The removed values, so the caller can report what was dropped.</returns>
    public IReadOnlyList<ProductValue> ChangeFamily(FamilySchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var removed = Values.Where(value => schema.FindAttribute(value.Attribute) is null).ToList();
        foreach (var value in removed)
        {
            _values.Remove((value.Attribute, value.Scope));
        }

        FamilyId = schema.Family.Id;
        return removed.AsReadOnly();
    }

    /// <summary>
    /// Validates every value against the current family and attribute definitions, for example after a constraint was
    /// tightened or an attribute was removed from the family.
    /// </summary>
    /// <param name="schema">The schema of the product's family.</param>
    /// <returns>
    /// Success, or every problem found: <c>product.attribute_not_in_family</c> for values of attributes the family no
    /// longer has, and the errors from <see cref="AttributeDefinition.Validate"/>. Every error's context includes the
    /// product's SKU.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="schema"/> is not the schema of the product's family.</exception>
    public Result Validate(FamilySchema schema)
    {
        EnsureOwnFamily(schema);

        var errors = new List<ResultError>();
        foreach (var ((attributeId, scope), value) in _values)
        {
            var definition = schema.FindAttribute(attributeId);
            if (definition is null)
            {
                errors.Add(ProductErrors.AttributeNotInFamily(this, schema.Family, attributeId.ToString()));
                continue;
            }

            errors.AddRange(definition.Validate(value, scope).Errors.Select(AddProductContext));
        }

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    /// <summary>
    /// Calculates how complete the product is for a channel: for each locale active in the channel, which of the
    /// attributes the family requires for the channel have a value.
    /// </summary>
    /// <remarks>
    /// A required attribute is filled when the product has a value in the matching scope: the locale if the attribute
    /// is localizable, and the channel if it is scopable. Only presence counts; use <see cref="Validate"/> to check
    /// that values are valid.
    /// </remarks>
    /// <param name="schema">The schema of the product's family.</param>
    /// <param name="channel">The channel.</param>
    /// <returns>The completeness per locale active in the channel.</returns>
    /// <exception cref="ArgumentException"><paramref name="schema"/> is not the schema of the product's family.</exception>
    public ChannelCompleteness CalculateCompleteness(FamilySchema schema, Channel channel)
    {
        EnsureOwnFamily(schema);
        ArgumentNullException.ThrowIfNull(channel);

        var required = schema.Family.RequiredAttributes(channel.Id)
            .Select(id => schema.FindAttribute(id))
            .OfType<AttributeDefinition>()
            .ToList();

        var locales = channel.Locales
            .Select(locale => new LocaleCompleteness(
                locale,
                required.Count,
                required
                    .Where(attribute => !_values.ContainsKey((attribute.Id, ScopeFor(attribute, locale, channel.Id))))
                    .Select(attribute => attribute.Code)
                    .ToList()))
            .ToList();

        return new ChannelCompleteness(channel.Id, locales);
    }

    private static ValueScope ScopeFor(AttributeDefinition attribute, Locale locale, ChannelId channel) =>
        new(attribute.IsLocalizable ? locale : null, attribute.IsScopable ? channel : null);

    private void EnsureOwnFamily(FamilySchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (schema.Family.Id != FamilyId)
        {
            throw new ArgumentException("The schema must be the schema of the product's own family.", nameof(schema));
        }
    }

    private ResultError AddProductContext(ResultError error) => error.WithContext(ErrorContextKeys.Product, Sku.Value);
}
