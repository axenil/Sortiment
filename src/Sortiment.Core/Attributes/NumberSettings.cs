using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings of <see cref="AttributeType.Number"/> attributes.
/// </summary>
public sealed class NumberSettings : AttributeSettings
{
    /// <summary>
    /// The largest number of decimal places a <see cref="decimal"/> can represent, and so the largest allowed
    /// <see cref="DecimalPlaces"/>.
    /// </summary>
    public const int MaxDecimalPlaces = 28;

    private readonly AttributeCode _attribute;

    internal NumberSettings(AttributeCode attribute) => _attribute = attribute;

    /// <summary>
    /// Gets the smallest allowed value, or <see langword="null"/> for no limit.
    /// </summary>
    public decimal? MinValue { get; private set; }

    /// <summary>
    /// Gets the largest allowed value, or <see langword="null"/> for no limit.
    /// </summary>
    public decimal? MaxValue { get; private set; }

    /// <summary>
    /// Gets the maximum number of decimal places, or <see langword="null"/> for no limit. Zero allows whole numbers
    /// only.
    /// </summary>
    public int? DecimalPlaces { get; private set; }

    /// <summary>
    /// Sets the range of allowed values. Both limits are inclusive.
    /// </summary>
    /// <param name="minValue">The smallest allowed value, or <see langword="null"/> for no lower limit.</param>
    /// <param name="maxValue">The largest allowed value, or <see langword="null"/> for no upper limit.</param>
    /// <returns>Success, or the error <c>attribute.invalid_range</c> if the minimum is greater than the maximum.</returns>
    public Result SetRange(decimal? minValue, decimal? maxValue)
    {
        if (minValue > maxValue)
        {
            return Result.Failure(AttributeErrors.InvalidRange(_attribute, minValue.Value, maxValue.Value));
        }

        MinValue = minValue;
        MaxValue = maxValue;
        return Result.Success();
    }

    /// <summary>
    /// Sets the maximum number of decimal places.
    /// </summary>
    /// <param name="decimalPlaces">
    /// The maximum number of decimal places, where zero allows whole numbers only, or <see langword="null"/> for no
    /// limit.
    /// </param>
    /// <returns>
    /// Success, or the error <c>attribute.invalid_decimal_places</c> if the value is outside
    /// 0–<see cref="MaxDecimalPlaces"/>.
    /// </returns>
    public Result SetDecimalPlaces(int? decimalPlaces)
    {
        if (decimalPlaces is < 0 or > MaxDecimalPlaces)
        {
            return Result.Failure(AttributeErrors.InvalidDecimalPlaces(_attribute, decimalPlaces.Value));
        }

        DecimalPlaces = decimalPlaces;
        return Result.Success();
    }

    internal override IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value) =>
        value is NumberValue number
            ? ValidateNumber(attribute, number.Value)
            : [ValueErrors.TypeMismatch(attribute, value)];

    private IEnumerable<ResultError> ValidateNumber(AttributeDefinition attribute, decimal number)
    {
        if (MinValue is { } minimum && number < minimum)
        {
            yield return ValueErrors.BelowMinimum(attribute, number, minimum);
        }

        if (MaxValue is { } maximum && number > maximum)
        {
            yield return ValueErrors.AboveMaximum(attribute, number, maximum);
        }

        if (DecimalPlaces is { } decimalPlaces && decimal.Round(number, decimalPlaces) != number)
        {
            yield return ValueErrors.TooManyDecimals(attribute, number, decimalPlaces);
        }
    }
}
