namespace Sortiment.Core.Measurements;

/// <summary>
/// The identity of a <see cref="MeasurementUnit"/>.
/// </summary>
public readonly record struct MeasurementUnitId
{
    /// <summary>
    /// Wraps an existing ID, for example one read from storage.
    /// </summary>
    /// <param name="value">The ID.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public MeasurementUnitId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A measurement unit ID cannot be empty.", nameof(value));
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
    public static MeasurementUnitId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
