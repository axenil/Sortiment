using System.Collections.ObjectModel;

namespace Sortiment.Core.Channels;

/// <summary>
/// A destination for product data, such as a web shop, a marketplace or a print catalog.
/// A channel decides which locales its product data must be available in.
/// </summary>
public sealed class Channel
{
    private readonly List<Locale> _locales;

    private Channel(ChannelId id, ChannelCode code, List<Locale> locales)
    {
        Id = id;
        Code = code;
        _locales = locales;
        Locales = _locales.AsReadOnly();
    }

    /// <summary>
    /// Gets the identity of the channel.
    /// </summary>
    public ChannelId Id { get; }

    /// <summary>
    /// Gets the code that imports, feeds and the API use to refer to the channel.
    /// </summary>
    public ChannelCode Code { get; }

    /// <summary>
    /// Gets the locales that are active in the channel, in display order. A channel always has at least one locale.
    /// </summary>
    public ReadOnlyCollection<Locale> Locales { get; }

    /// <summary>
    /// Creates a new channel.
    /// </summary>
    /// <param name="code">The channel's code.</param>
    /// <param name="locales">The locales that are active in the channel, in display order.</param>
    /// <returns>
    /// The new channel, or the errors <c>channel.locale_required</c> if no locale is given and
    /// <c>channel.locale_already_active</c> for each duplicate locale.
    /// </returns>
    public static Result<Channel> Create(ChannelCode code, IEnumerable<Locale> locales)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(locales);

        var errors = new List<ResultError>();
        var distinctLocales = new List<Locale>();
        foreach (var locale in locales)
        {
            ArgumentNullException.ThrowIfNull(locale, nameof(locales));
            if (distinctLocales.Contains(locale))
            {
                errors.Add(ChannelErrors.LocaleAlreadyActive(code, locale));
            }
            else
            {
                distinctLocales.Add(locale);
            }
        }

        if (distinctLocales.Count == 0)
        {
            errors.Add(ChannelErrors.LocaleRequired(code));
        }

        return errors.Count == 0
            ? Result.Success(new Channel(ChannelId.New(), code, distinctLocales))
            : Result.Failure<Channel>(errors);
    }

    /// <summary>
    /// Gets a value indicating whether a locale is active in the channel.
    /// </summary>
    /// <param name="locale">The locale.</param>
    /// <returns><see langword="true"/> if the locale is active.</returns>
    public bool HasLocale(Locale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);

        return _locales.Contains(locale);
    }

    /// <summary>
    /// Activates a locale in the channel, after the existing locales.
    /// </summary>
    /// <param name="locale">The locale to activate.</param>
    /// <returns>Success, or the error <c>channel.locale_already_active</c>.</returns>
    public Result AddLocale(Locale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);

        if (HasLocale(locale))
        {
            return Result.Failure(ChannelErrors.LocaleAlreadyActive(Code, locale));
        }

        _locales.Add(locale);
        return Result.Success();
    }

    /// <summary>
    /// Deactivates a locale in the channel.
    /// </summary>
    /// <param name="locale">The locale to deactivate.</param>
    /// <returns>
    /// Success, or the error <c>channel.locale_not_active</c> if the locale is not active, or
    /// <c>channel.locale_required</c> if it is the channel's only locale.
    /// </returns>
    public Result RemoveLocale(Locale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);

        if (!HasLocale(locale))
        {
            return Result.Failure(ChannelErrors.LocaleNotActive(Code, locale));
        }

        if (_locales.Count == 1)
        {
            return Result.Failure(ChannelErrors.LocaleRequired(Code));
        }

        _locales.Remove(locale);
        return Result.Success();
    }
}
