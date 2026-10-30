namespace Sortiment.Core.Products;

/// <summary>
/// The identity of a <see cref="Product"/>.
/// </summary>
public readonly record struct ProductId
{
    /// <summary>
    /// Wraps an existing ID, for example one read from storage.
    /// </summary>
    /// <param name="value">The ID.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public ProductId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A product ID cannot be empty.", nameof(value));
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
    public static ProductId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
