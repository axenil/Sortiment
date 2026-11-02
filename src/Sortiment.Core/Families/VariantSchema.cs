using Sortiment.Core.Attributes;

namespace Sortiment.Core.Families;

/// <summary>
/// A family variant together with the schema of its family, resolved by the caller. Operations on product models and
/// variant products take a variant schema, in the same way that product operations take a <see cref="FamilySchema"/>
/// (ADR 0015).
/// </summary>
/// <remarks>
/// Only attributes that are still in the family count. A variant attribute that was removed from the family is
/// neither a variant attribute nor a model attribute.
/// </remarks>
public sealed class VariantSchema
{
    /// <summary>
    /// Combines a family schema and a family variant of the same family.
    /// </summary>
    /// <param name="familySchema">The schema of the family.</param>
    /// <param name="familyVariant">The family variant.</param>
    /// <exception cref="ArgumentException">The family variant belongs to another family.</exception>
    public VariantSchema(FamilySchema familySchema, FamilyVariant familyVariant)
    {
        ArgumentNullException.ThrowIfNull(familySchema);
        ArgumentNullException.ThrowIfNull(familyVariant);
        if (familyVariant.FamilyId != familySchema.Family.Id)
        {
            throw new ArgumentException(
                "The family variant must belong to the family of the schema.",
                nameof(familyVariant));
        }

        FamilySchema = familySchema;
        FamilyVariant = familyVariant;
    }

    /// <summary>
    /// Gets the schema of the family.
    /// </summary>
    public FamilySchema FamilySchema { get; }

    /// <summary>
    /// Gets the family variant.
    /// </summary>
    public FamilyVariant FamilyVariant { get; }

    /// <summary>
    /// Gets the definitions of the axes that are still in the family, in order.
    /// </summary>
    public IReadOnlyList<AttributeDefinition> Axes =>
        FamilyVariant.Axes
            .Select(FamilySchema.FindAttribute)
            .Where(definition => definition is not null)
            .Select(definition => definition!)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether an attribute of the family is set per variant.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <returns><see langword="true"/> if the attribute is in the family and is a variant attribute.</returns>
    public bool IsVariantAttribute(AttributeId attribute) =>
        FamilySchema.FindAttribute(attribute) is not null && FamilyVariant.IsVariantAttribute(attribute);

    /// <summary>
    /// Gets a value indicating whether an attribute of the family is shared by the variants of a product model.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <returns><see langword="true"/> if the attribute is in the family and is not a variant attribute.</returns>
    public bool IsModelAttribute(AttributeId attribute) =>
        FamilySchema.FindAttribute(attribute) is not null && !FamilyVariant.IsVariantAttribute(attribute);
}
