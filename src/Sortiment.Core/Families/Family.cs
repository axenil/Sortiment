using System.Collections.ObjectModel;
using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;

namespace Sortiment.Core.Families;

/// <summary>
/// A kind of product, such as t-shirts or televisions. A family decides which attributes its products have and which
/// of them must be filled in before a product is complete for a channel.
/// </summary>
/// <remarks>
/// The family refers to attributes and channels by ID. It cannot see products, so callers that remove an attribute
/// from a family are responsible for removing the matching values from its products.
/// </remarks>
public sealed class Family
{
    private readonly List<AttributeId> _attributes = [];
    private readonly Dictionary<ChannelId, HashSet<AttributeId>> _requirements = [];

    private Family(FamilyId id, FamilyCode code)
    {
        Id = id;
        Code = code;
        Attributes = _attributes.AsReadOnly();
    }

    /// <summary>
    /// Gets the identity of the family.
    /// </summary>
    public FamilyId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the family.
    /// </summary>
    public FamilyCode Code { get; }

    /// <summary>
    /// Gets the attributes of the family's products, in display order.
    /// </summary>
    public ReadOnlyCollection<AttributeId> Attributes { get; }

    /// <summary>
    /// Gets the channels that have requirements, each with the attributes it requires.
    /// </summary>
    public IReadOnlyDictionary<ChannelId, IReadOnlySet<AttributeId>> Requirements =>
        _requirements.ToDictionary(pair => pair.Key, pair => (IReadOnlySet<AttributeId>)pair.Value.ToHashSet());

    /// <summary>
    /// Creates a new family without attributes.
    /// </summary>
    /// <param name="code">The family's code.</param>
    /// <returns>The new family.</returns>
    public static Result<Family> Create(FamilyCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Result.Success(new Family(FamilyId.New(), code));
    }

    /// <summary>
    /// Gets a value indicating whether an attribute is in the family.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <returns><see langword="true"/> if the attribute is in the family.</returns>
    public bool HasAttribute(AttributeId attribute) => _attributes.Contains(attribute);

    /// <summary>
    /// Adds an attribute to the end of the family's attributes.
    /// </summary>
    /// <param name="attribute">The attribute to add.</param>
    /// <returns>Success, or the error <c>family.attribute_already_in_family</c>.</returns>
    public Result AddAttribute(AttributeDefinition attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        if (HasAttribute(attribute.Id))
        {
            return Result.Failure(FamilyErrors.AttributeAlreadyInFamily(Code, attribute.Code));
        }

        _attributes.Add(attribute.Id);
        return Result.Success();
    }

    /// <summary>
    /// Removes an attribute from the family, together with its requirements.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <returns>Success, or the error <c>family.attribute_not_in_family</c>.</returns>
    public Result RemoveAttribute(AttributeId attribute)
    {
        if (!_attributes.Remove(attribute))
        {
            return Result.Failure(FamilyErrors.AttributeNotInFamily(Code, attribute.ToString()));
        }

        foreach (var required in _requirements.Values)
        {
            required.Remove(attribute);
        }

        return Result.Success();
    }

    /// <summary>
    /// Requires an attribute to have a value before a product is complete for a channel. Requiring an attribute that
    /// is already required succeeds.
    /// </summary>
    /// <param name="attribute">The attribute to require.</param>
    /// <param name="channel">The channel that requires it.</param>
    /// <returns>Success, or the error <c>family.attribute_not_in_family</c>.</returns>
    public Result RequireAttribute(AttributeDefinition attribute, ChannelId channel)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        if (!HasAttribute(attribute.Id))
        {
            return Result.Failure(FamilyErrors.AttributeNotInFamily(Code, attribute.Code.Value));
        }

        if (!_requirements.TryGetValue(channel, out var required))
        {
            required = [];
            _requirements[channel] = required;
        }

        required.Add(attribute.Id);
        return Result.Success();
    }

    /// <summary>
    /// Stops requiring an attribute for a channel. Doing so for an attribute that is not required has no effect.
    /// </summary>
    /// <param name="attribute">The attribute's ID.</param>
    /// <param name="channel">The channel.</param>
    public void UnrequireAttribute(AttributeId attribute, ChannelId channel)
    {
        if (_requirements.TryGetValue(channel, out var required))
        {
            required.Remove(attribute);
        }
    }

    /// <summary>
    /// Gets the attributes a channel requires, in the family's attribute order.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The required attributes. Empty if the channel has no requirements.</returns>
    public IReadOnlyList<AttributeId> RequiredAttributes(ChannelId channel) =>
        _requirements.TryGetValue(channel, out var required)
            ? _attributes.Where(required.Contains).ToList().AsReadOnly()
            : [];
}
