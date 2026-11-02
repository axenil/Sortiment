using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Families;
using Sortiment.Core.Values;

namespace Sortiment.Core.Products;

/// <summary>
/// A product: a SKU, a family, and values for the family's attributes. A product with a <see cref="ProductModelId"/>
/// is a variant of that product model (ADR 0015).
/// </summary>
/// <remarks>
/// Operations that need attribute definitions take the <see cref="FamilySchema"/> of the product's family, resolved by
/// the caller (ADR 0010). Operations on a variant take a <see cref="VariantContext"/> instead, because a variant has values
/// only for variant attributes and shares the rest with its model. SKU uniqueness, and the uniqueness of each variant's
/// <see cref="AxisCombination"/> within its model, are enforced by whoever stores products.
/// </remarks>
public sealed class Product
{
    private readonly ValueSet _values = new();

    private Product(ProductId id, Sku sku, FamilyId familyId, ProductModelId? productModelId)
    {
        Id = id;
        Sku = sku;
        FamilyId = familyId;
        ProductModelId = productModelId;
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
    /// Gets the identity of the product model this product is a variant of, or <see langword="null"/> for a product
    /// without a model. It cannot change after the product is created.
    /// </summary>
    public ProductModelId? ProductModelId { get; }

    /// <summary>
    /// Gets a value indicating whether the product is a variant of a product model.
    /// </summary>
    public bool IsVariant => ProductModelId is not null;

    /// <summary>
    /// Gets a value indicating whether the product is enabled. Disabled products are kept but not published to channels.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Gets the product's values.
    /// </summary>
    public IReadOnlyList<ProductValue> Values => _values.Values;

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

        return Result.Success(new Product(ProductId.New(), sku, schema.Family.Id, productModelId: null));
    }

    /// <summary>
    /// Creates a new, enabled variant of a product model, without values. Its axis values are set with
    /// <see cref="SetValue(VariantContext, AttributeCode, ValueScope, AttributeValue)"/>; until every axis has a value,
    /// <see cref="Validate(VariantContext)"/> reports the variant as invalid.
    /// </summary>
    /// <param name="sku">The variant's SKU.</param>
    /// <param name="context">The product model and the schema of its family variant.</param>
    /// <returns>The new variant.</returns>
    public static Result<Product> CreateVariant(Sku sku, VariantContext context)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(context);

        return Result.Success(new Product(
            ProductId.New(),
            sku,
            context.Schema.FamilySchema.Family.Id,
            context.Model.Id));
    }

    /// <summary>
    /// Restores a product from stored state (ADR 0011). Values are not validated against their attributes, so values
    /// that break a constraint tightened later can still be loaded; <see cref="Validate(FamilySchema)"/> reports them.
    /// </summary>
    /// <param name="id">The product's ID.</param>
    /// <param name="sku">The product's SKU.</param>
    /// <param name="familyId">The identity of the product's family.</param>
    /// <param name="isEnabled">Whether the product is enabled.</param>
    /// <param name="values">The product's values.</param>
    /// <param name="productModelId">The identity of the product model, if the product is a variant.</param>
    /// <returns>The product.</returns>
    /// <exception cref="ArgumentException">There is more than one value for the same attribute and scope.</exception>
    public static Product Restore(
        ProductId id,
        Sku sku,
        FamilyId familyId,
        bool isEnabled,
        IEnumerable<ProductValue> values,
        ProductModelId? productModelId = null)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(values);

        var product = new Product(id, sku, familyId, productModelId) { IsEnabled = isEnabled };
        var problems = new RestoreProblems();
        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(values));
            ArgumentNullException.ThrowIfNull(value.Scope, nameof(values));
            ArgumentNullException.ThrowIfNull(value.Value, nameof(values));
            if (!product._values.TryRestore(value))
            {
                problems.Add($"Attribute {value.Attribute} has more than one value in scope {value.Scope}.");
            }
        }

        problems.ThrowIfAny($"Product '{sku}'", nameof(values));
        return product;
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

        return _values.Get(attribute, scope);
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
            return Result.Failure(ProductErrors.AttributeNotInFamily(this, schema.Family, attribute));
        }

        var result = _values.SetIfValid(definition, scope, value);
        return result.IsSuccess ? result : Result.Failure(result.Errors.Select(AddProductContext));
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

        return _values.Remove(attribute, scope);
    }

    /// <summary>
    /// Moves the product to another family. Values for attributes that the new family does not have are removed.
    /// </summary>
    /// <param name="schema">The schema of the new family.</param>
    /// <returns>The removed values, so the caller can report what was dropped.</returns>
    /// <exception cref="InvalidOperationException">
    /// The product is a variant. A variant's family is its model's family (ADR 0015).
    /// </exception>
    public IReadOnlyList<ProductValue> ChangeFamily(FamilySchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (IsVariant)
        {
            throw new InvalidOperationException(
                $"Product '{Sku}' is a variant, so its family is the family of its product model and cannot be changed.");
        }

        var removed = Values.Where(value => schema.FindAttribute(value.Attribute) is null).ToList();
        foreach (var value in removed)
        {
            _values.Remove(value.Attribute, value.Scope);
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
        foreach (var (attributeId, scope, value) in _values.Values)
        {
            var definition = schema.FindAttribute(attributeId);
            if (definition is null)
            {
                errors.Add(ProductErrors.AttributeNotInFamily(this, schema.Family, attributeId));
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
    /// is localizable, and the channel if it is scopable. Only presence counts; use <see cref="Validate(FamilySchema)"/> to check
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

        return CompletenessCalculator.Calculate(schema.RequiredAttributes(channel.Id), channel, HasValue);
    }

    /// <summary>
    /// Sets the value of a variant attribute on a variant, replacing any value already there.
    /// </summary>
    /// <param name="context">The variant's product model and the schema of its family variant.</param>
    /// <param name="attribute">The attribute's code.</param>
    /// <param name="scope">The scope of the value.</param>
    /// <param name="value">The value.</param>
    /// <returns>
    /// Success, or the error <c>product.attribute_not_in_family</c>, <c>product.attribute_on_model</c> for a model
    /// attribute, or the attribute's validation errors.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="context"/> does not hold the variant's own model.</exception>
    public Result SetValue(VariantContext context, AttributeCode attribute, ValueScope scope, AttributeValue value)
    {
        EnsureOwnModel(context);
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(value);

        var schema = context.Schema;
        var definition = schema.FamilySchema.FindAttribute(attribute);
        if (definition is null)
        {
            return Result.Failure(ProductErrors.AttributeNotInFamily(this, schema.FamilySchema.Family, attribute));
        }

        if (schema.IsModelAttribute(definition.Id))
        {
            return Result.Failure(ProductErrors.AttributeOnModel(this, schema.FamilyVariant, attribute));
        }

        var result = _values.SetIfValid(definition, scope, value);
        return result.IsSuccess ? result : Result.Failure(result.Errors.Select(AddProductContext));
    }

    /// <summary>
    /// Validates a variant against the current schema: every value of the variant, and that every axis has a value.
    /// The model's own values are validated by <see cref="ProductModel.Validate"/>.
    /// </summary>
    /// <param name="context">The variant's product model and the schema of its family variant.</param>
    /// <returns>
    /// Success, or every error found: <c>product.attribute_not_in_family</c>, <c>product.attribute_on_model</c>,
    /// <c>product.axis_value_missing</c> and the attributes' validation errors.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="context"/> does not hold the variant's own model.</exception>
    public Result Validate(VariantContext context)
    {
        EnsureOwnModel(context);

        var schema = context.Schema;
        var errors = new List<ResultError>();
        foreach (var (attributeId, scope, value) in _values.Values)
        {
            var definition = schema.FamilySchema.FindAttribute(attributeId);
            if (definition is null)
            {
                errors.Add(ProductErrors.AttributeNotInFamily(this, schema.FamilySchema.Family, attributeId));
            }
            else if (schema.IsModelAttribute(attributeId))
            {
                errors.Add(ProductErrors.AttributeOnModel(this, schema.FamilyVariant, attributeId));
            }
            else
            {
                errors.AddRange(definition.Validate(value, scope).Errors.Select(AddProductContext));
            }
        }

        errors.AddRange(schema.Axes
            .Where(axis => !HasValue(axis.Id, ValueScope.Global))
            .Select(axis => ProductErrors.AxisValueMissing(this, schema.FamilyVariant, axis.Code)));

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    /// <summary>
    /// Calculates how complete a variant is for a channel, counting both its own values and its model's values.
    /// </summary>
    /// <param name="context">The variant's product model and the schema of its family variant.</param>
    /// <param name="channel">The channel.</param>
    /// <returns>The completeness per locale active in the channel.</returns>
    /// <exception cref="ArgumentException"><paramref name="context"/> does not hold the variant's own model.</exception>
    public ChannelCompleteness CalculateCompleteness(VariantContext context, Channel channel)
    {
        EnsureOwnModel(context);
        ArgumentNullException.ThrowIfNull(channel);

        return CompletenessCalculator.Calculate(
            context.Schema.FamilySchema.RequiredAttributes(channel.Id),
            channel,
            (attribute, scope) => HasValue(attribute, scope) || context.Model.HasValue(attribute, scope));
    }

    /// <summary>
    /// Gets a variant's effective values: its own values together with its model's values (ADR 0015).
    /// </summary>
    /// <param name="model">The variant's product model.</param>
    /// <returns>The variant's values followed by the model's values.</returns>
    /// <exception cref="ArgumentException"><paramref name="model"/> is not the variant's own model.</exception>
    public IReadOnlyList<ProductValue> EffectiveValues(ProductModel model)
    {
        EnsureOwnModel(model);

        return Values.Concat(model.Values).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the effective value of an attribute in a scope: the variant's own value, or else its model's value.
    /// </summary>
    /// <param name="model">The variant's product model.</param>
    /// <param name="attribute">The attribute's ID.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>The value, or <see langword="null"/> if neither the variant nor its model has a value there.</returns>
    /// <exception cref="ArgumentException"><paramref name="model"/> is not the variant's own model.</exception>
    public AttributeValue? GetEffectiveValue(ProductModel model, AttributeId attribute, ValueScope scope)
    {
        EnsureOwnModel(model);
        ArgumentNullException.ThrowIfNull(scope);

        return _values.Get(attribute, scope) ?? model.GetValue(attribute, scope);
    }

    /// <summary>
    /// Gets the combination of a variant's axis values, which must be unique among the variants of its model.
    /// </summary>
    /// <param name="context">The variant's product model and the schema of its family variant.</param>
    /// <returns>The combination, or <see langword="null"/> if an axis has no value yet.</returns>
    /// <exception cref="ArgumentException"><paramref name="context"/> does not hold the variant's own model.</exception>
    public AxisCombination? AxisCombination(VariantContext context)
    {
        EnsureOwnModel(context);

        var values = new List<ProductValue>();
        foreach (var axis in context.Schema.FamilyVariant.Axes)
        {
            if (_values.Get(axis, ValueScope.Global) is not { } value)
            {
                return null;
            }

            values.Add(new ProductValue(axis, ValueScope.Global, value));
        }

        return new AxisCombination(values);
    }

    private bool HasValue(AttributeId attribute, ValueScope scope) => _values.Has(attribute, scope);

    private void EnsureOwnFamily(FamilySchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (schema.Family.Id != FamilyId)
        {
            throw new ArgumentException("The schema must be the schema of the product's own family.", nameof(schema));
        }

        if (IsVariant)
        {
            throw new ArgumentException(
                $"Product '{Sku}' is a variant. Pass a {nameof(VariantContext)} instead of a family schema.",
                nameof(schema));
        }
    }

    private void EnsureOwnModel(VariantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Model.Id != ProductModelId)
        {
            throw new ArgumentException("The context must hold the product's own product model.", nameof(context));
        }
    }

    private void EnsureOwnModel(ProductModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Id != ProductModelId)
        {
            throw new ArgumentException("The model must be the product's own product model.", nameof(model));
        }
    }

    private ResultError AddProductContext(ResultError error) => error.WithContext(ErrorContextKeys.Product, Sku.Value);
}
