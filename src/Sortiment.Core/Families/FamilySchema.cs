using System.Collections.ObjectModel;
using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;

namespace Sortiment.Core.Families;

/// <summary>
/// A family together with the definitions of its attributes, resolved by the caller. Product operations take a schema
/// so that domain types never have to look anything up.
/// </summary>
/// <remarks>
/// A schema is meant to be created for one unit of work, such as a request or an import, and reused for every product
/// in it. It reads the family's attributes and requirements from the family itself, so attributes removed from the
/// family since the schema was created are left out. An attribute added to the family since then has no definition in
/// the schema, so the schema is no longer <see cref="IsCurrent">current</see> and refuses to answer: its members throw
/// <see cref="InvalidOperationException"/> rather than leave the attribute out (ADR 0017). Create a new schema instead.
/// </remarks>
public sealed class FamilySchema
{
    private readonly Dictionary<AttributeId, AttributeDefinition> _attributesById;
    private readonly Dictionary<AttributeCode, AttributeDefinition> _attributesByCode;
    private int _checkedAttributesVersion;

    private FamilySchema(Family family, AttributeDefinition[] attributes)
    {
        Family = family;
        _attributesById = attributes.ToDictionary(attribute => attribute.Id);
        _attributesByCode = attributes.ToDictionary(attribute => attribute.Code);
        _checkedAttributesVersion = family.AttributesVersion;
    }

    /// <summary>
    /// Gets the family.
    /// </summary>
    public Family Family { get; }

    /// <summary>
    /// Gets a value indicating whether the schema has a definition for every attribute of the family. It becomes
    /// <see langword="false"/> when an attribute is added to the family after the schema was created; the schema's
    /// other members then throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public bool IsCurrent
    {
        get
        {
            if (Family.AttributesVersion == _checkedAttributesVersion)
            {
                return true;
            }

            if (Family.Attributes.Any(id => !_attributesById.ContainsKey(id)))
            {
                return false;
            }

            _checkedAttributesVersion = Family.AttributesVersion;
            return true;
        }
    }

    /// <summary>
    /// Gets the definitions of the family's attributes, in the family's attribute order.
    /// </summary>
    /// <exception cref="InvalidOperationException">The schema is not <see cref="IsCurrent">current</see>.</exception>
    public ReadOnlyCollection<AttributeDefinition> Attributes
    {
        get
        {
            EnsureCurrent();
            return Family.Attributes.Select(id => _attributesById[id]).ToList().AsReadOnly();
        }
    }

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
    /// Gets the definitions of the attributes the family requires for a channel, in the family's attribute order.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The required attributes. Empty if the channel has no requirements.</returns>
    /// <exception cref="InvalidOperationException">The schema is not <see cref="IsCurrent">current</see>.</exception>
    public IReadOnlyList<AttributeDefinition> RequiredAttributes(ChannelId channel)
    {
        EnsureCurrent();
        return Family.RequiredAttributes(channel).Select(id => _attributesById[id]).ToList().AsReadOnly();
    }

    /// <summary>
    /// Finds one of the family's attributes by ID.
    /// </summary>
    /// <param name="id">The attribute's ID.</param>
    /// <returns>The attribute, or <see langword="null"/> if it is not in the family.</returns>
    /// <exception cref="InvalidOperationException">The schema is not <see cref="IsCurrent">current</see>.</exception>
    public AttributeDefinition? FindAttribute(AttributeId id)
    {
        EnsureCurrent();
        return Family.HasAttribute(id) ? _attributesById[id] : null;
    }

    /// <summary>
    /// Finds one of the family's attributes by code.
    /// </summary>
    /// <param name="code">The attribute's code.</param>
    /// <returns>The attribute, or <see langword="null"/> if it is not in the family.</returns>
    /// <exception cref="InvalidOperationException">The schema is not <see cref="IsCurrent">current</see>.</exception>
    public AttributeDefinition? FindAttribute(AttributeCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        EnsureCurrent();

        return _attributesByCode.TryGetValue(code, out var attribute) && Family.HasAttribute(attribute.Id)
            ? attribute
            : null;
    }

    /// <summary>
    /// Throws unless the schema is current. Product, product model and variant operations call this first, so they
    /// refuse a stale schema whether or not they would have looked up an attribute.
    /// </summary>
    /// <exception cref="InvalidOperationException">The schema is not current.</exception>
    internal void EnsureCurrent()
    {
        if (IsCurrent)
        {
            return;
        }

        var missing = Family.Attributes.Where(id => !_attributesById.ContainsKey(id));
        throw new InvalidOperationException(
            $"The schema of family '{Family.Code}' is out of date: it has no definition for the attributes with IDs " +
            $"{string.Join(", ", missing)}, which were added to the family after the schema was created. " +
            "Create a new schema.");
    }
}
