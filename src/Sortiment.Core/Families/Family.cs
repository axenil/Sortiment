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
    /// Restores a family from stored state (ADR 0011).
    /// </summary>
    /// <param name="id">The family's ID.</param>
    /// <param name="code">The family's code.</param>
    /// <param name="attributes">The IDs of the family's attributes, in display order.</param>
    /// <param name="requirements">The attributes each channel requires, as in <see cref="Requirements"/>.</param>
    /// <returns>The family.</returns>
    /// <exception cref="ArgumentException">
    /// An attribute is listed more than once, or a channel requires an attribute that is not in the family.
    /// </exception>
    public static Family Restore(
        FamilyId id,
        FamilyCode code,
        IEnumerable<AttributeId> attributes,
        IReadOnlyDictionary<ChannelId, IReadOnlySet<AttributeId>> requirements)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(requirements);

        var family = new Family(id, code);
        var problems = new List<string>();
        foreach (var attribute in attributes)
        {
            if (family.HasAttribute(attribute))
            {
                problems.Add($"Attribute {attribute} is listed more than once.");
            }
            else
            {
                family._attributes.Add(attribute);
            }
        }

        foreach (var (channel, required) in requirements)
        {
            ArgumentNullException.ThrowIfNull(required, nameof(requirements));
            problems.AddRange(required
                .Where(attribute => !family.HasAttribute(attribute))
                .Select(attribute => $"Channel {channel} requires attribute {attribute}, which is not in the family."));
            family._requirements[channel] = required.Where(family.HasAttribute).ToHashSet();
        }

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Family '{code}' cannot be restored. {string.Join(" ", problems)}",
                nameof(attributes));
        }

        return family;
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
            return Result.Failure(FamilyErrors.AttributeNotInFamily(Code, attribute));
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
            return Result.Failure(FamilyErrors.AttributeNotInFamily(Code, attribute.Code));
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
