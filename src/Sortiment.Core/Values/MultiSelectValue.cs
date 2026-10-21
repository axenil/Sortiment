using Sortiment.Core.Attributes;

namespace Sortiment.Core.Values;

/// <summary>
/// A value for a <see cref="AttributeType.MultiSelect"/> attribute: one or more of the attribute's options.
/// </summary>
/// <remarks>
/// Two values are equal when they select the same options, in any order. Display order comes from the attribute.
/// </remarks>
public sealed record MultiSelectValue : AttributeValue
{
    /// <summary>
    /// Creates a multi-select value.
    /// </summary>
    /// <param name="options">The IDs of the selected options.</param>
    public MultiSelectValue(IEnumerable<AttributeOptionId> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options.ToArray().AsReadOnly();
    }

    /// <summary>
    /// Gets the IDs of the selected options, in the order they were given.
    /// </summary>
    public IReadOnlyList<AttributeOptionId> Options { get; }

    /// <inheritdoc />
    public bool Equals(MultiSelectValue? other) =>
        other is not null && Options.ToHashSet().SetEquals(other.Options);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Options.Distinct().Aggregate(0, (hash, option) => hash ^ option.GetHashCode());
}
