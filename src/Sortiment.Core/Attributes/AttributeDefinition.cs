using System.Collections.ObjectModel;
using Sortiment.Core.Assets;
using Sortiment.Core.Measurements;
using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// Defines a product attribute: what kind of value it holds, which values are valid, and whether its values vary by
/// locale or channel.
/// </summary>
/// <remarks>
/// The code, type and scoping flags are fixed when the attribute is created, because changing them would
/// invalidate values that products already have. Constraints such as <see cref="MaxLength"/> can change; values that
/// break a tightened constraint are reported the next time they are validated.
/// </remarks>
public sealed class AttributeDefinition
{
    /// <summary>
    /// The largest number of decimal places a <see cref="decimal"/> can represent, and so the largest allowed
    /// <see cref="DecimalPlaces"/>.
    /// </summary>
    public const int MaxDecimalPlaces = 28;

    private readonly List<AttributeOption> _options = [];

    private AttributeDefinition(
        AttributeId id,
        AttributeCode code,
        AttributeType type,
        bool isLocalizable,
        bool isScopable,
        MeasurementFamily? measurementFamily = null,
        AssetFamilyId? assetFamilyId = null)
    {
        Id = id;
        Code = code;
        Type = type;
        IsLocalizable = isLocalizable;
        IsScopable = isScopable;
        MeasurementFamily = measurementFamily;
        AssetFamilyId = assetFamilyId;
        Options = _options.AsReadOnly();
    }

    /// <summary>
    /// Gets the identity of the attribute.
    /// </summary>
    public AttributeId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the attribute.
    /// </summary>
    public AttributeCode Code { get; }

    /// <summary>
    /// Gets the kind of value the attribute holds.
    /// </summary>
    public AttributeType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the attribute has a separate value per locale, for example a translated name.
    /// </summary>
    public bool IsLocalizable { get; }

    /// <summary>
    /// Gets a value indicating whether the attribute has a separate value per channel, for example a shorter
    /// description for a marketplace.
    /// </summary>
    public bool IsScopable { get; }

    /// <summary>
    /// Gets a value indicating whether the attribute's values are chosen from <see cref="Options"/>.
    /// </summary>
    public bool HasOptions => Type is AttributeType.SingleSelect or AttributeType.MultiSelect;

    /// <summary>
    /// Gets the options of a select attribute, in display order. Empty for other attribute types.
    /// </summary>
    public ReadOnlyCollection<AttributeOption> Options { get; }

    /// <summary>
    /// Gets the measurement family of a measurement attribute, which decides the units its values can use.
    /// <see langword="null"/> for other attribute types.
    /// </summary>
    public MeasurementFamily? MeasurementFamily { get; }

    /// <summary>
    /// Gets the asset family whose assets an asset collection attribute holds.
    /// <see langword="null"/> for other attribute types.
    /// </summary>
    public AssetFamilyId? AssetFamilyId { get; }

    /// <summary>
    /// Gets the maximum number of assets in an asset collection value, or <see langword="null"/> for no limit.
    /// </summary>
    public int? MaxCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the attribute holds text and so supports <see cref="MaxLength"/>.
    /// </summary>
    public bool IsTextual => Type is AttributeType.Text or AttributeType.TextArea or AttributeType.Identifier;

    /// <summary>
    /// Gets the maximum length of a textual value, counted in Unicode code points, or <see langword="null"/> for no limit.
    /// </summary>
    public int? MaxLength { get; private set; }

    /// <summary>
    /// Gets the smallest allowed value of a number attribute, or <see langword="null"/> for no limit.
    /// </summary>
    public decimal? MinValue { get; private set; }

    /// <summary>
    /// Gets the largest allowed value of a number attribute, or <see langword="null"/> for no limit.
    /// </summary>
    public decimal? MaxValue { get; private set; }

    /// <summary>
    /// Gets the maximum number of decimal places of a number attribute, or <see langword="null"/> for no limit.
    /// Zero allows whole numbers only.
    /// </summary>
    public int? DecimalPlaces { get; private set; }

    /// <summary>
    /// Creates a new attribute.
    /// </summary>
    /// <param name="code">The attribute's code.</param>
    /// <param name="type">The kind of value the attribute holds.</param>
    /// <param name="isLocalizable">Whether the attribute has a separate value per locale.</param>
    /// <param name="isScopable">Whether the attribute has a separate value per channel.</param>
    /// <returns>
    /// The new attribute, or the errors <c>attribute.identifier_cannot_be_localizable</c> and
    /// <c>attribute.identifier_cannot_be_scopable</c> if an identifier attribute is scoped.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not a defined attribute type.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="type"/> is <see cref="AttributeType.Measurement"/> or <see cref="AttributeType.AssetCollection"/>,
    /// which need a family. Use <see cref="CreateMeasurement"/> or <see cref="CreateAssetCollection"/> instead.
    /// </exception>
    public static Result<AttributeDefinition> Create(
        AttributeCode code,
        AttributeType type,
        bool isLocalizable = false,
        bool isScopable = false)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown attribute type.");
        }

        if (type == AttributeType.Measurement)
        {
            throw new ArgumentException(
                $"A measurement attribute needs a measurement family. Use {nameof(CreateMeasurement)} instead.",
                nameof(type));
        }

        if (type == AttributeType.AssetCollection)
        {
            throw new ArgumentException(
                $"An asset collection attribute needs an asset family. Use {nameof(CreateAssetCollection)} instead.",
                nameof(type));
        }

        var errors = new List<ResultError>();
        if (type == AttributeType.Identifier && isLocalizable)
        {
            errors.Add(AttributeErrors.IdentifierCannotBeLocalizable(code));
        }

        if (type == AttributeType.Identifier && isScopable)
        {
            errors.Add(AttributeErrors.IdentifierCannotBeScopable(code));
        }

        return errors.Count == 0
            ? Result.Success(new AttributeDefinition(AttributeId.New(), code, type, isLocalizable, isScopable))
            : Result.Failure<AttributeDefinition>(errors);
    }

    /// <summary>
    /// Creates a new measurement attribute. The measurement family cannot change after the attribute is created.
    /// </summary>
    /// <param name="code">The attribute's code.</param>
    /// <param name="measurementFamily">
    /// The family whose units the attribute's values use, for example <see cref="MeasurementFamilies.Weight"/>.
    /// </param>
    /// <param name="isLocalizable">Whether the attribute has a separate value per locale.</param>
    /// <param name="isScopable">Whether the attribute has a separate value per channel.</param>
    /// <returns>The new attribute.</returns>
    public static Result<AttributeDefinition> CreateMeasurement(
        AttributeCode code,
        MeasurementFamily measurementFamily,
        bool isLocalizable = false,
        bool isScopable = false)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(measurementFamily);

        return Result.Success(new AttributeDefinition(
            AttributeId.New(),
            code,
            AttributeType.Measurement,
            isLocalizable,
            isScopable,
            measurementFamily: measurementFamily));
    }

    /// <summary>
    /// Creates a new asset collection attribute. The asset family cannot change after the attribute is created.
    /// </summary>
    /// <param name="code">The attribute's code.</param>
    /// <param name="assetFamily">The family whose assets the attribute's values hold.</param>
    /// <param name="isLocalizable">Whether the attribute has a separate value per locale.</param>
    /// <param name="isScopable">Whether the attribute has a separate value per channel.</param>
    /// <returns>The new attribute.</returns>
    public static Result<AttributeDefinition> CreateAssetCollection(
        AttributeCode code,
        AssetFamily assetFamily,
        bool isLocalizable = false,
        bool isScopable = false)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(assetFamily);

        return Result.Success(new AttributeDefinition(
            AttributeId.New(),
            code,
            AttributeType.AssetCollection,
            isLocalizable,
            isScopable,
            assetFamilyId: assetFamily.Id));
    }

    /// <summary>
    /// Sets the maximum length of a textual attribute's values.
    /// </summary>
    /// <param name="maxLength">The maximum length in Unicode code points, or <see langword="null"/> for no limit.</param>
    /// <returns>
    /// Success, or the error <c>attribute.constraint_not_supported</c> if the attribute is not textual, or
    /// <c>attribute.invalid_max_length</c> if <paramref name="maxLength"/> is less than 1.
    /// </returns>
    public Result SetMaxLength(int? maxLength)
    {
        if (!IsTextual)
        {
            return Result.Failure(AttributeErrors.ConstraintNotSupported(Code, Type, "a maximum length"));
        }

        if (maxLength < 1)
        {
            return Result.Failure(AttributeErrors.InvalidMaxLength(Code, maxLength.Value));
        }

        MaxLength = maxLength;
        return Result.Success();
    }

    /// <summary>
    /// Sets the range of a number attribute's values. Both limits are inclusive.
    /// </summary>
    /// <param name="minValue">The smallest allowed value, or <see langword="null"/> for no lower limit.</param>
    /// <param name="maxValue">The largest allowed value, or <see langword="null"/> for no upper limit.</param>
    /// <returns>
    /// Success, or the error <c>attribute.constraint_not_supported</c> if this is not a number attribute, or
    /// <c>attribute.invalid_range</c> if the minimum is greater than the maximum.
    /// </returns>
    public Result SetRange(decimal? minValue, decimal? maxValue)
    {
        if (Type != AttributeType.Number)
        {
            return Result.Failure(AttributeErrors.ConstraintNotSupported(Code, Type, "a range"));
        }

        if (minValue > maxValue)
        {
            return Result.Failure(AttributeErrors.InvalidRange(Code, minValue.Value, maxValue.Value));
        }

        MinValue = minValue;
        MaxValue = maxValue;
        return Result.Success();
    }

    /// <summary>
    /// Sets the maximum number of decimal places of a number attribute's values.
    /// </summary>
    /// <param name="decimalPlaces">
    /// The maximum number of decimal places, where zero allows whole numbers only, or <see langword="null"/> for no limit.
    /// </param>
    /// <returns>
    /// Success, or the error <c>attribute.constraint_not_supported</c> if this is not a number attribute, or
    /// <c>attribute.invalid_decimal_places</c> if the value is outside 0–<see cref="MaxDecimalPlaces"/>.
    /// </returns>
    public Result SetDecimalPlaces(int? decimalPlaces)
    {
        if (Type != AttributeType.Number)
        {
            return Result.Failure(AttributeErrors.ConstraintNotSupported(Code, Type, "decimal places"));
        }

        if (decimalPlaces is < 0 or > MaxDecimalPlaces)
        {
            return Result.Failure(AttributeErrors.InvalidDecimalPlaces(Code, decimalPlaces.Value));
        }

        DecimalPlaces = decimalPlaces;
        return Result.Success();
    }

    /// <summary>
    /// Sets the maximum number of assets in an asset collection attribute's values.
    /// </summary>
    /// <param name="maxCount">The maximum number of assets, or <see langword="null"/> for no limit.</param>
    /// <returns>
    /// Success, or the error <c>attribute.constraint_not_supported</c> if this is not an asset collection attribute,
    /// or <c>attribute.invalid_max_count</c> if <paramref name="maxCount"/> is less than 1.
    /// </returns>
    public Result SetMaxCount(int? maxCount)
    {
        if (Type != AttributeType.AssetCollection)
        {
            return Result.Failure(AttributeErrors.ConstraintNotSupported(Code, Type, "a maximum count"));
        }

        if (maxCount < 1)
        {
            return Result.Failure(AttributeErrors.InvalidMaxCount(Code, maxCount.Value));
        }

        MaxCount = maxCount;
        return Result.Success();
    }

    /// <summary>
    /// Checks whether a value can be stored for this attribute in the given scope.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="scope">The locale and channel the value applies to.</param>
    /// <returns>
    /// Success, or every problem found: errors from <see cref="ValidateScope"/>, plus <c>value.type_mismatch</c>,
    /// <c>value.empty</c>, <c>value.line_break_not_allowed</c>, <c>value.surrounding_whitespace</c>,
    /// <c>value.too_long</c>, <c>value.below_minimum</c>, <c>value.above_maximum</c>, <c>value.too_many_decimals</c>,
    /// <c>value.unknown_option</c>, <c>value.duplicate_option</c>, <c>value.unknown_unit</c>,
    /// <c>value.duplicate_asset</c> or <c>value.too_many_assets</c>.
    /// Whether the assets of an asset collection exist and belong to <see cref="AssetFamilyId"/> is not checked here,
    /// because that needs the assets themselves.
    /// Each error's context includes the attribute and, when present, the scope's locale and channel.
    /// </returns>
    public Result Validate(AttributeValue value, ValueScope scope)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(scope);

        var errors = ScopeErrors(scope)
            .Concat(AttributeValueValidator.Validate(this, value))
            .Select(error => AddScopeContext(error, scope))
            .ToList();

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    /// <summary>
    /// Checks whether a scope fits this attribute: localizable attributes need a locale, scopable attributes need
    /// a channel, and other attributes must not have them.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns>
    /// Success, or the errors <c>value.locale_required</c>, <c>value.locale_not_allowed</c>,
    /// <c>value.channel_required</c> or <c>value.channel_not_allowed</c>.
    /// </returns>
    public Result ValidateScope(ValueScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var errors = ScopeErrors(scope).Select(error => AddScopeContext(error, scope)).ToList();
        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    /// <summary>
    /// Adds an option to the end of a select attribute's option list.
    /// </summary>
    /// <param name="code">The option's code, unique within this attribute.</param>
    /// <returns>
    /// The new option, or the error <c>attribute.options_not_supported</c> if this is not a select attribute,
    /// or <c>attribute.option_already_exists</c> if the code is taken.
    /// </returns>
    public Result<AttributeOption> AddOption(AttributeOptionCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (!HasOptions)
        {
            return Result.Failure<AttributeOption>(AttributeErrors.OptionsNotSupported(Code, Type));
        }

        if (FindOption(code) is not null)
        {
            return Result.Failure<AttributeOption>(AttributeErrors.OptionAlreadyExists(Code, code));
        }

        var option = new AttributeOption(AttributeOptionId.New(), code);
        _options.Add(option);
        return Result.Success(option);
    }

    /// <summary>
    /// Removes an option from a select attribute.
    /// </summary>
    /// <remarks>
    /// The attribute cannot see product values, so callers must make sure no product still uses the option.
    /// </remarks>
    /// <param name="code">The code of the option to remove.</param>
    /// <returns>Success, or the error <c>attribute.option_not_found</c> if the attribute has no such option.</returns>
    public Result RemoveOption(AttributeOptionCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var option = FindOption(code);
        if (option is null)
        {
            return Result.Failure(AttributeErrors.OptionNotFound(Code, code));
        }

        _options.Remove(option);
        return Result.Success();
    }

    /// <summary>
    /// Finds an option by code.
    /// </summary>
    /// <param name="code">The option's code.</param>
    /// <returns>The option, or <see langword="null"/> if the attribute has no such option.</returns>
    public AttributeOption? FindOption(AttributeOptionCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _options.Find(option => option.Code == code);
    }

    /// <summary>
    /// Finds an option by ID.
    /// </summary>
    /// <param name="id">The option's ID.</param>
    /// <returns>The option, or <see langword="null"/> if the attribute has no such option.</returns>
    public AttributeOption? FindOption(AttributeOptionId id) => _options.Find(option => option.Id == id);

    private IEnumerable<ResultError> ScopeErrors(ValueScope scope)
    {
        if (IsLocalizable && scope.Locale is null)
        {
            yield return ValueErrors.LocaleRequired(this);
        }

        if (!IsLocalizable && scope.Locale is not null)
        {
            yield return ValueErrors.LocaleNotAllowed(this);
        }

        if (IsScopable && scope.ChannelId is null)
        {
            yield return ValueErrors.ChannelRequired(this);
        }

        if (!IsScopable && scope.ChannelId is not null)
        {
            yield return ValueErrors.ChannelNotAllowed(this);
        }
    }

    private static ResultError AddScopeContext(ResultError error, ValueScope scope)
    {
        if (scope.Locale is { } locale)
        {
            error = error.WithContext(ErrorContextKeys.Locale, locale.Code);
        }

        if (scope.ChannelId is { } channelId)
        {
            error = error.WithContext(ErrorContextKeys.Channel, channelId.ToString());
        }

        return error;
    }
}
