using System.Collections.ObjectModel;
using Sortiment.Core.Channels;

namespace Sortiment.Core.Products;

/// <summary>
/// How complete a product is for a channel, per locale active in the channel.
/// </summary>
public sealed class ChannelCompleteness
{
    internal ChannelCompleteness(ChannelId channelId, IList<LocaleCompleteness> locales)
    {
        ChannelId = channelId;
        Locales = new ReadOnlyCollection<LocaleCompleteness>(locales);
    }

    /// <summary>
    /// Gets the identity of the channel.
    /// </summary>
    public ChannelId ChannelId { get; }

    /// <summary>
    /// Gets the completeness per locale, in the channel's locale order.
    /// </summary>
    public ReadOnlyCollection<LocaleCompleteness> Locales { get; }

    /// <summary>
    /// Gets a value indicating whether the product is complete in every locale of the channel.
    /// </summary>
    public bool IsComplete => Locales.All(locale => locale.IsComplete);

    /// <summary>
    /// Gets the completeness for one locale.
    /// </summary>
    /// <param name="locale">The locale.</param>
    /// <returns>The completeness, or <see langword="null"/> if the locale is not active in the channel.</returns>
    public LocaleCompleteness? ForLocale(Locale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);

        return Locales.FirstOrDefault(completeness => completeness.Locale == locale);
    }
}
