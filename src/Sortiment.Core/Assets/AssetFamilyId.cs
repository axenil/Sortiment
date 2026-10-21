namespace Sortiment.Core.Assets;

/// <summary>
/// The identity of an <see cref="AssetFamily"/>.
/// </summary>
public readonly record struct AssetFamilyId
{
    /// <summary>
    /// Wraps an existing ID, for example one read from storage.
    /// </summary>
    /// <param name="value">The ID.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public AssetFamilyId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An asset family ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying GUID.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new, time-ordered ID.
    /// </summary>
    /// <returns>A new ID.</returns>
    public static AssetFamilyId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
