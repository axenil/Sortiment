using System.Collections.ObjectModel;
using Sortiment.Core.Attributes;

namespace Sortiment.Core.Families;

/// <summary>
/// A family together with the definitions of its attributes, resolved by the caller. Product operations take a schema
/// so that domain types never have to look anything up.
/// </summary>
/// <remarks>
/// A schema is meant to be created for one unit of work, such as a request or an import, and reused for every product
/// in it. It always reflects attributes removed from the family since it was created, but attributes added since then
/// are only included in a new schema.
/// </remarks>
public sealed class FamilySchema
{
    private readonly Dictionary<AttributeId, AttributeDefinition> _attributesById;
    private readonly Dictionary<AttributeCode, AttributeDefinition> _attributesByCode;

    private FamilySchema(Family family, AttributeDefinition[] attributes)
    {
        Family = family;
        _attributesById = attributes.ToDictionary(attribute => attribute.Id);
        _attributesByCode = attributes.ToDictionary(attribute => attribute.Code);
    }

    /// <summary>
    /// Gets the family.
    /// </summary>
    public Family Family { get; }

    /// <summary>
    /// Gets the definitions of the family's attributes, in the family's attribute order.
    /// </summary>
    public ReadOnlyCollection<AttributeDefinition> Attributes =>
        Family.Attributes.Where(_attributesById.ContainsKey).Select(id => _attributesById[id]).ToList().AsReadOnly();

    /// <summary>
    /// Resolves a family's attributes.
    /// </summary>
    /// <param name="family">The family.</param>
    /// <param name="attributes">
    /// Attribute definitions that include every attribute of the family. Definitions of other attributes are ignored,
    /// so callers can pass a larger set.
    /// </param>
    /// <returns>
    /// The schema, or the error <c>family_schema.attribute_missing</c> for each family attribute without a definition.
    /// </returns>
    public static Result<FamilySchema> Create(Family family, IEnumerable<AttributeDefinition> attributes)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(attributes);

        var available = new Dictionary<AttributeId, AttributeDefinition>();
        foreach (var attribute in attributes)
        {
            ArgumentNullException.ThrowIfNull(attribute, nameof(attributes));
            available.TryAdd(attribute.Id, attribute);
        }

        var missing = family.Attributes.Where(id => !available.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return Result.Failure<FamilySchema>(
                missing.Select(id => FamilyErrors.AttributeDefinitionMissing(family.Code, id)));
        }

        return Result.Success(new FamilySchema(family, family.Attributes.Select(id => available[id]).ToArray()));
    }

    /// <summary>
    /// Finds one of the family's attributes by ID.
    /// </summary>
    /// <param name="id">The attribute's ID.</param>
    /// <returns>The attribute, or <see langword="null"/> if it is not in the family.</returns>
    public AttributeDefinition? FindAttribute(AttributeId id) =>
        Family.HasAttribute(id) ? _attributesById.GetValueOrDefault(id) : null;

    /// <summary>
    /// Finds one of the family's attributes by code.
    /// </summary>
    /// <param name="code">The attribute's code.</param>
    /// <returns>The attribute, or <see langword="null"/> if it is not in the family.</returns>
    public AttributeDefinition? FindAttribute(AttributeCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _attributesByCode.TryGetValue(code, out var attribute) && Family.HasAttribute(attribute.Id)
            ? attribute
            : null;
    }
}
