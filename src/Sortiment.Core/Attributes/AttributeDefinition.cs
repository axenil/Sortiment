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
/// invalidate values that products already have. Settings that only apply to one kind of attribute, such as a maximum
/// length or the options of a select attribute, live in <see cref="Settings"/>. Constraints in the settings can
/// change; values that break a tightened constraint are reported the next time they are validated.
/// </remarks>
public sealed class AttributeDefinition
{
    private AttributeDefinition(
        AttributeCode code,
        AttributeType type,
        bool isLocalizable,
        bool isScopable,
        AttributeSettings settings)
    {
        Id = AttributeId.New();
        Code = code;
        Type = type;
        IsLocalizable = isLocalizable;
        IsScopable = isScopable;
        Settings = settings;
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
    /// Gets the settings for the attribute's kind: <see cref="TextSettings"/> for text, text area and identifier
    /// attributes, <see cref="SelectSettings"/> for single and multi-select attributes, and one settings type for each
    /// other attribute type.
    /// </summary>
    public AttributeSettings Settings { get; }

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

        var settings = CreateSettings(code, type);
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
            ? Result.Success(new AttributeDefinition(code, type, isLocalizable, isScopable, settings))
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
            code,
            AttributeType.Measurement,
            isLocalizable,
            isScopable,
            new MeasurementSettings(measurementFamily)));
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
            code,
            AttributeType.AssetCollection,
            isLocalizable,
            isScopable,
            new AssetCollectionSettings(code, assetFamily.Id)));
    }

    /// <summary>
    /// Gets the settings as the type that matches the attribute's kind, for example
    /// <c>attribute.SettingsAs&lt;TextSettings&gt;().SetMaxLength(150)</c>.
    /// </summary>
    /// <typeparam name="TSettings">The expected settings type.</typeparam>
    /// <returns>The settings.</returns>
    /// <exception cref="InvalidOperationException">The attribute's settings are of another type.</exception>
    public TSettings SettingsAs<TSettings>()
        where TSettings : AttributeSettings =>
        Settings as TSettings
        ?? throw new InvalidOperationException(
            $"Attribute '{Code}' has type {Type}, so its settings are {Settings.GetType().Name}, not {typeof(TSettings).Name}.");

    /// <summary>
    /// Checks whether a value can be stored for this attribute in the given scope.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="scope">The locale and channel the value applies to.</param>
    /// <returns>
    /// Success, or every problem found: errors from <see cref="ValidateScope"/>, <c>value.type_mismatch</c>, and the
    /// errors described by the attribute's <see cref="Settings"/> type.
    /// Each error's context includes the attribute and, when present, the scope's locale and channel.
    /// </returns>
    public Result Validate(AttributeValue value, ValueScope scope)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(scope);

        var errors = ScopeErrors(scope)
            .Concat(Settings.ValidateValue(this, value))
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

    private static AttributeSettings CreateSettings(AttributeCode code, AttributeType type) =>
        type switch
        {
            AttributeType.Text or AttributeType.TextArea or AttributeType.Identifier => new TextSettings(code),
            AttributeType.Boolean => new BooleanSettings(),
            AttributeType.Number => new NumberSettings(code),
            AttributeType.Date => new DateSettings(),
            AttributeType.SingleSelect or AttributeType.MultiSelect => new SelectSettings(code),
            AttributeType.Measurement => throw new ArgumentException(
                $"A measurement attribute needs a measurement family. Use {nameof(CreateMeasurement)} instead.",
                nameof(type)),
            AttributeType.AssetCollection => throw new ArgumentException(
                $"An asset collection attribute needs an asset family. Use {nameof(CreateAssetCollection)} instead.",
                nameof(type)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown attribute type."),
        };

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
