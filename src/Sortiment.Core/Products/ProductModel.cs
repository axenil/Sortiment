using Sortiment.Core.Attributes;
using Sortiment.Core.Families;
using Sortiment.Core.Values;

namespace Sortiment.Core.Products;

/// <summary>
/// The data that the variants of a product share, such as the name, description and material of a t-shirt that comes
/// in several colors and sizes (ADR 0015). A product model is not sold or sent to channels itself; its variants are.
/// </summary>
/// <remarks>
/// A product model has values only for the model attributes of its family variant. Operations that need attribute
/// definitions take the <see cref="VariantSchema"/> of its family variant, resolved by the caller. Code uniqueness is
/// enforced by whoever stores product models.
/// </remarks>
public sealed class ProductModel
{
    private readonly ValueSet _values = new();

    private ProductModel(ProductModelId id, ProductModelCode code, FamilyVariantId familyVariantId)
    {
        Id = id;
        Code = code;
        FamilyVariantId = familyVariantId;
    }

    /// <summary>
    /// Gets the identity of the product model.
    /// </summary>
    public ProductModelId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the product model.
    /// </summary>
    public ProductModelCode Code { get; }

    /// <summary>
    /// Gets the identity of the family variant, which decides the model's family and which attributes are set on the
    /// model. It cannot change after the model is created.
    /// </summary>
    public FamilyVariantId FamilyVariantId { get; }

    /// <summary>
    /// Gets the model's values, which its variants share.
    /// </summary>
    public IReadOnlyList<ProductValue> Values => _values.Values;

    /// <summary>
    /// Creates a new product model without values.
    /// </summary>
    /// <param name="code">The product model's code.</param>
    /// <param name="schema">The schema of the family variant the model uses.</param>
    /// <returns>The new product model.</returns>
    public static Result<ProductModel> Create(ProductModelCode code, VariantSchema schema)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(schema);

        return Result.Success(new ProductModel(ProductModelId.New(), code, schema.FamilyVariant.Id));
    }

    /// <summary>
    /// Restores a product model from stored state (ADR 0011). Values are not validated against their attributes, so
    /// values that break a constraint tightened later, or that are at the wrong level after the family variant
    /// changed, can still be loaded; <see cref="Validate"/> reports them.
    /// </summary>
    /// <param name="id">The product model's ID.</param>
    /// <param name="code">The product model's code.</param>
    /// <param name="familyVariantId">The identity of its family variant.</param>
    /// <param name="values">The model's values.</param>
    /// <returns>The product model.</returns>
    /// <exception cref="ArgumentException">There is more than one value for the same attribute and scope.</exception>
    public static ProductModel Restore(
        ProductModelId id,
        ProductModelCode code,
        FamilyVariantId familyVariantId,
        IEnumerable<ProductValue> values)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(values);

        var model = new ProductModel(id, code, familyVariantId);
        var problems = new RestoreProblems();
        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(values));
            ArgumentNullException.ThrowIfNull(value.Scope, nameof(values));
            ArgumentNullException.ThrowIfNull(value.Value, nameof(values));
            if (!model._values.TryRestore(value))
            {
                problems.Add($"Attribute {value.Attribute} has more than one value in scope {value.Scope}.");
            }
        }

        problems.ThrowIfAny($"Product model '{code}'", nameof(values));
        return model;
    }

    /// <summary>
    /// Gets the value of an attribute in a scope.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>The value, or <see langword="null"/> if the model has no value there.</returns>
    public AttributeValue? GetValue(AttributeId attribute, ValueScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _values.Get(attribute, scope);
    }

    /// <summary>
    /// Sets the value of a model attribute in a scope, replacing any value already there.
    /// </summary>
    /// <param name="schema">The schema of the model's family variant.</param>
    /// <param name="attribute">The attribute's code.</param>
    /// <param name="scope">The scope of the value.</param>
    /// <param name="value">The value.</param>
    /// <returns>
    /// Success, or the error <c>product_model.attribute_not_in_family</c>,
    /// <c>product_model.attribute_not_on_model</c> for a variant attribute, or the attribute's validation errors.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="schema"/> is not the schema of the model's family variant.</exception>
    public Result SetValue(VariantSchema schema, AttributeCode attribute, ValueScope scope, AttributeValue value)
    {
        EnsureOwnFamilyVariant(schema);
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(value);

        var definition = schema.FamilySchema.FindAttribute(attribute);
        if (definition is null)
        {
            return Result.Failure(ProductModelErrors.AttributeNotInFamily(this, schema.FamilySchema.Family, attribute));
        }

        if (schema.IsVariantAttribute(definition.Id))
        {
            return Result.Failure(ProductModelErrors.AttributeNotOnModel(this, schema.FamilyVariant, attribute));
        }

        var result = _values.SetIfValid(definition, scope, value);
        return result.IsSuccess ? result : Result.Failure(result.Errors.Select(AddModelContext));
    }

    /// <summary>
    /// Removes the value of an attribute in a scope.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <param name="scope">The scope.</param>
    /// <returns><see langword="true"/> if there was a value to remove.</returns>
    public bool RemoveValue(AttributeId attribute, ValueScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return _values.Remove(attribute, scope);
    }

    /// <summary>
    /// Validates every value of the model against the current schema. This reports values that break a constraint
    /// tightened after they were set, values of attributes no longer in the family, and values of attributes that
    /// became variant attributes.
    /// </summary>
    /// <param name="schema">The schema of the model's family variant.</param>
    /// <returns>
    /// Success, or every error found: <c>product_model.attribute_not_in_family</c>,
    /// <c>product_model.attribute_not_on_model</c> and the attributes' validation errors.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="schema"/> is not the schema of the model's family variant.</exception>
    public Result Validate(VariantSchema schema)
    {
        EnsureOwnFamilyVariant(schema);

        var errors = new List<ResultError>();
        foreach (var (attributeId, scope, value) in _values.Values)
        {
            var definition = schema.FamilySchema.FindAttribute(attributeId);
            if (definition is null)
            {
                errors.Add(ProductModelErrors.AttributeNotInFamily(this, schema.FamilySchema.Family, attributeId));
            }
            else if (schema.IsVariantAttribute(attributeId))
            {
                errors.Add(ProductModelErrors.AttributeNotOnModel(this, schema.FamilyVariant, attributeId));
            }
            else
            {
                errors.AddRange(definition.Validate(value, scope).Errors.Select(AddModelContext));
            }
        }

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    internal bool HasValue(AttributeId attribute, ValueScope scope) => _values.Has(attribute, scope);

    private void EnsureOwnFamilyVariant(VariantSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (schema.FamilyVariant.Id != FamilyVariantId)
        {
            throw new ArgumentException(
                "The schema must be the schema of the product model's own family variant.",
                nameof(schema));
        }
    }

    private ResultError AddModelContext(ResultError error) =>
        error.WithContext(ErrorContextKeys.ProductModel, Code.Value);
}
