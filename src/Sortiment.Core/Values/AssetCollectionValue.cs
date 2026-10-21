using Sortiment.Core.Assets;

namespace Sortiment.Core.Values;

/// <summary>
/// A value for an <see cref="Attributes.AttributeType.AssetCollection"/> attribute: one or more assets, in order.
/// </summary>
/// <remarks>
/// Order is meaningful: the first asset is the main one, for example the main product image. Two values are equal
/// only when they hold the same assets in the same order.
/// </remarks>
public sealed record AssetCollectionValue : AttributeValue
{
    /// <summary>
    /// Creates an asset collection value.
    /// </summary>
    /// <param name="assets">The IDs of the assets, in order.</param>
    public AssetCollectionValue(IEnumerable<AssetId> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

        Assets = assets.ToArray().AsReadOnly();
    }

    /// <summary>
    /// Gets the IDs of the assets, in order.
    /// </summary>
    public IReadOnlyList<AssetId> Assets { get; }

    /// <inheritdoc />
    public bool Equals(AssetCollectionValue? other) =>
        other is not null && Assets.SequenceEqual(other.Assets);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var asset in Assets)
        {
            hash.Add(asset);
        }

        return hash.ToHashCode();
    }
}
