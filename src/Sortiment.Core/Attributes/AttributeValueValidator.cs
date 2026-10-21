using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// Checks a value's content against its attribute: the type match, the attribute's constraints and its options.
/// </summary>
internal static class AttributeValueValidator
{
    public static IEnumerable<ResultError> Validate(AttributeDefinition attribute, AttributeValue value) =>
        (attribute.Type, value) switch
        {
            (AttributeType.Text, TextValue text) => ValidateText(attribute, text.Text),
            (AttributeType.TextArea, TextValue text) => ValidateText(attribute, text.Text),
            (AttributeType.Identifier, TextValue text) => ValidateText(attribute, text.Text),
            (AttributeType.Boolean, BooleanValue) => [],
            (AttributeType.Number, NumberValue number) => ValidateNumber(attribute, number.Value),
            (AttributeType.Date, DateValue) => [],
            (AttributeType.SingleSelect, SingleSelectValue single) => ValidateSingleSelect(attribute, single),
            (AttributeType.MultiSelect, MultiSelectValue multi) => ValidateMultiSelect(attribute, multi),
            (AttributeType.Measurement, MeasurementValue measurement) => ValidateMeasurement(attribute, measurement),
            (AttributeType.AssetCollection, AssetCollectionValue assets) => ValidateAssetCollection(attribute, assets),
            _ => [ValueErrors.TypeMismatch(attribute, value)],
        };

    private static IEnumerable<ResultError> ValidateAssetCollection(
        AttributeDefinition attribute,
        AssetCollectionValue value)
    {
        if (value.Assets.Count == 0)
        {
            yield return ValueErrors.Empty(attribute);
            yield break;
        }

        var duplicates = value.Assets
            .GroupBy(asset => asset)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
        foreach (var duplicate in duplicates)
        {
            yield return ValueErrors.DuplicateAsset(attribute, duplicate);
        }

        if (attribute.MaxCount is { } maxCount && value.Assets.Count > maxCount)
        {
            yield return ValueErrors.TooManyAssets(attribute, value.Assets.Count, maxCount);
        }
    }

    private static IEnumerable<ResultError> ValidateMeasurement(AttributeDefinition attribute, MeasurementValue value)
    {
        var family = attribute.MeasurementFamily!;
        if (family.FindUnit(value.Unit) is null)
        {
            yield return ValueErrors.UnknownUnit(attribute, family, value.Unit);
        }
    }

    private static IEnumerable<ResultError> ValidateText(AttributeDefinition attribute, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return ValueErrors.Empty(attribute);
            yield break;
        }

        if (attribute.Type != AttributeType.TextArea && text.AsSpan().ContainsAny('\r', '\n'))
        {
            yield return ValueErrors.LineBreakNotAllowed(attribute);
        }

        if (attribute.Type == AttributeType.Identifier && text.Trim().Length != text.Length)
        {
            yield return ValueErrors.SurroundingWhitespace(attribute);
        }

        // Length is counted in Unicode code points, like PostgreSQL's varchar(n) and most channel specifications,
        // so an emoji counts as one character rather than two UTF-16 code units.
        var length = text.EnumerateRunes().Count();
        if (attribute.MaxLength is { } maxLength && length > maxLength)
        {
            yield return ValueErrors.TooLong(attribute, length, maxLength);
        }
    }

    private static IEnumerable<ResultError> ValidateNumber(AttributeDefinition attribute, decimal number)
    {
        if (attribute.MinValue is { } minimum && number < minimum)
        {
            yield return ValueErrors.BelowMinimum(attribute, number, minimum);
        }

        if (attribute.MaxValue is { } maximum && number > maximum)
        {
            yield return ValueErrors.AboveMaximum(attribute, number, maximum);
        }

        if (attribute.DecimalPlaces is { } decimalPlaces && decimal.Round(number, decimalPlaces) != number)
        {
            yield return ValueErrors.TooManyDecimals(attribute, number, decimalPlaces);
        }
    }

    private static IEnumerable<ResultError> ValidateSingleSelect(AttributeDefinition attribute, SingleSelectValue value)
    {
        if (attribute.FindOption(value.Option) is null)
        {
            yield return ValueErrors.UnknownOption(attribute, value.Option);
        }
    }

    private static IEnumerable<ResultError> ValidateMultiSelect(AttributeDefinition attribute, MultiSelectValue value)
    {
        if (value.Options.Count == 0)
        {
            yield return ValueErrors.Empty(attribute);
            yield break;
        }

        var seen = new HashSet<AttributeOptionId>();
        var reportedDuplicates = new HashSet<AttributeOptionId>();
        foreach (var option in value.Options)
        {
            if (!seen.Add(option))
            {
                if (reportedDuplicates.Add(option))
                {
                    yield return ValueErrors.DuplicateOption(attribute, option);
                }

                continue;
            }

            if (attribute.FindOption(option) is null)
            {
                yield return ValueErrors.UnknownOption(attribute, option);
            }
        }
    }
}
