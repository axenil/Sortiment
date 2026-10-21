using System.Collections.ObjectModel;

namespace Sortiment.Core.Attributes;

/// <summary>
/// Defines a product attribute: what kind of value it holds and whether its values vary by locale or channel.
/// </summary>
/// <remarks>
/// The code, type and scoping flags are fixed when the attribute is created, because changing them would
/// invalidate values that products already have.
/// </remarks>
public sealed class AttributeDefinition
{
    private readonly List<AttributeOption> _options = [];

    private AttributeDefinition(
        AttributeId id,
        AttributeCode code,
        AttributeType type,
        bool isLocalizable,
        bool isScopable)
    {
        Id = id;
        Code = code;
        Type = type;
        IsLocalizable = isLocalizable;
        IsScopable = isScopable;
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
}
