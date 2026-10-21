using System.Collections.ObjectModel;
using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings of <see cref="AttributeType.SingleSelect"/> and <see cref="AttributeType.MultiSelect"/> attributes:
/// the options their values are chosen from.
/// </summary>
public sealed class SelectSettings : AttributeSettings
{
    private readonly AttributeCode _attribute;
    private readonly List<AttributeOption> _options = [];

    internal SelectSettings(AttributeCode attribute)
    {
        _attribute = attribute;
        Options = _options.AsReadOnly();
    }

    /// <summary>
    /// Gets the options, in display order.
    /// </summary>
    public ReadOnlyCollection<AttributeOption> Options { get; }

    /// <summary>
    /// Adds an option to the end of the option list.
    /// </summary>
    /// <param name="code">The option's code, unique within the attribute.</param>
    /// <returns>The new option, or the error <c>attribute.option_already_exists</c> if the code is taken.</returns>
    public Result<AttributeOption> AddOption(AttributeOptionCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (FindOption(code) is not null)
        {
            return Result.Failure<AttributeOption>(AttributeErrors.OptionAlreadyExists(_attribute, code));
        }

        var option = new AttributeOption(AttributeOptionId.New(), code);
        _options.Add(option);
        return Result.Success(option);
    }

    /// <summary>
    /// Removes an option.
    /// </summary>
    /// <remarks>
    /// The attribute cannot see product values, so callers must make sure no product still uses the option.
    /// </remarks>
    /// <param name="code">The code of the option to remove.</param>
    /// <returns>Success, or the error <c>attribute.option_not_found</c> if there is no such option.</returns>
    public Result RemoveOption(AttributeOptionCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var option = FindOption(code);
        if (option is null)
        {
            return Result.Failure(AttributeErrors.OptionNotFound(_attribute, code));
        }

        _options.Remove(option);
        return Result.Success();
    }

    /// <summary>
    /// Finds an option by code.
    /// </summary>
    /// <param name="code">The option's code.</param>
    /// <returns>The option, or <see langword="null"/> if there is no such option.</returns>
    public AttributeOption? FindOption(AttributeOptionCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _options.Find(option => option.Code == code);
    }

    /// <summary>
    /// Finds an option by ID.
    /// </summary>
    /// <param name="id">The option's ID.</param>
    /// <returns>The option, or <see langword="null"/> if there is no such option.</returns>
    public AttributeOption? FindOption(AttributeOptionId id) => _options.Find(option => option.Id == id);

    internal override IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value) =>
        (attribute.Type, value) switch
        {
            (AttributeType.SingleSelect, SingleSelectValue single) => ValidateSingleSelect(attribute, single),
            (AttributeType.MultiSelect, MultiSelectValue multi) => ValidateMultiSelect(attribute, multi),
            _ => [ValueErrors.TypeMismatch(attribute, value)],
        };

    private IEnumerable<ResultError> ValidateSingleSelect(AttributeDefinition attribute, SingleSelectValue value)
    {
        if (FindOption(value.Option) is null)
        {
            yield return ValueErrors.UnknownOption(attribute, value.Option);
        }
    }

    private IEnumerable<ResultError> ValidateMultiSelect(AttributeDefinition attribute, MultiSelectValue value)
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
                    yield return ValueErrors.DuplicateOption(attribute, option, FindOption(option));
                }

                continue;
            }

            if (FindOption(option) is null)
            {
                yield return ValueErrors.UnknownOption(attribute, option);
            }
        }
    }
}
