using Sortiment.Core.Values;

namespace Sortiment.Core.Products;

/// <summary>
/// The axis values of a variant product, which tell it apart from the other variants of its product model
/// (ADR 0015). Two variants of the same model must not have equal combinations.
/// </summary>
/// <remarks>
/// Uniqueness is enforced by the application service and storage, because a variant cannot see its siblings.
/// <see cref="Key"/> gives storage a stable value to index.
/// </remarks>
public sealed class AxisCombination : IEquatable<AxisCombination>
{
    internal AxisCombination(IList<ProductValue> values)
    {
        Values = values.AsReadOnly();
        Key = string.Join(";", values.Select(value => $"{value.Attribute}={KeyOf(value.Value)}"));
    }

    /// <summary>
    /// Gets the value of each axis, in axis order. Axis values are always global.
    /// </summary>
    public IReadOnlyList<ProductValue> Values { get; }

    /// <summary>
    /// Gets a stable text form of the combination, made of the axis IDs and the selected option IDs or boolean
    /// values. Equal combinations have equal keys.
    /// </summary>
    public string Key { get; }

    /// <inheritdoc />
    public bool Equals(AxisCombination? other) => other is not null && Key == other.Key;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as AxisCombination);

    /// <inheritdoc />
    public override int GetHashCode() => Key.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc />
    public override string ToString() => Key;

    private static string KeyOf(AttributeValue value) =>
        value switch
        {
            SingleSelectValue select => select.Option.ToString(),
            BooleanValue boolean => boolean.Value ? "true" : "false",
            _ => value.ToString() ?? string.Empty,
        };
}
