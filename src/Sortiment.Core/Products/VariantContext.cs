using Sortiment.Core.Families;

namespace Sortiment.Core.Products;

/// <summary>
/// What operations on a variant product need, resolved by the caller: the schema of its family variant and its product
/// model (ADR 0015). One context can be reused for every variant of the model.
/// </summary>
public sealed class VariantContext
{
    /// <summary>
    /// Combines a variant schema with a product model of that family variant.
    /// </summary>
    /// <param name="schema">The schema of the model's family variant.</param>
    /// <param name="model">The product model.</param>
    /// <exception cref="ArgumentException">The model uses another family variant.</exception>
    public VariantContext(VariantSchema schema, ProductModel model)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(model);
        if (model.FamilyVariantId != schema.FamilyVariant.Id)
        {
            throw new ArgumentException("The product model must use the family variant of the schema.", nameof(model));
        }

        Schema = schema;
        Model = model;
    }

    /// <summary>
    /// Gets the schema of the model's family variant.
    /// </summary>
    public VariantSchema Schema { get; }

    /// <summary>
    /// Gets the product model.
    /// </summary>
    public ProductModel Model { get; }
}
