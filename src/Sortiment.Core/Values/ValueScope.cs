using Sortiment.Core.Channels;

namespace Sortiment.Core.Values;

/// <summary>
/// Where a value applies: to one locale, one channel, both, or everywhere.
/// </summary>
/// <remarks>
/// A localizable attribute needs a locale, and a scopable attribute needs a channel. Other attributes must use
/// <see cref="Global"/>.
/// </remarks>
/// <param name="Locale">The locale the value applies to, or <see langword="null"/> for every locale.</param>
/// <param name="ChannelId">The channel the value applies to, or <see langword="null"/> for every channel.</param>
public sealed record ValueScope(Locale? Locale, ChannelId? ChannelId)
{
    /// <summary>
    /// Gets the scope of a value that applies to every locale and channel.
    /// </summary>
    public static ValueScope Global { get; } = new(null, null);

    /// <summary>
    /// Creates the scope of a value that applies to one locale in every channel.
    /// </summary>
    /// <param name="locale">The locale.</param>
    /// <returns>The scope.</returns>
    public static ValueScope ForLocale(Locale locale)
    {
        ArgumentNullException.ThrowIfNull(locale);

        return new(locale, null);
    }

    /// <summary>
    /// Creates the scope of a value that applies to one channel in every locale.
    /// </summary>
    /// <param name="channelId">The channel.</param>
    /// <returns>The scope.</returns>
    public static ValueScope ForChannel(ChannelId channelId) => new(null, channelId);

    /// <summary>
    /// Creates the scope of a value that applies to one locale in one channel.
    /// </summary>
    /// <param name="locale">The locale.</param>
    /// <param name="channelId">The channel.</param>
    /// <returns>The scope.</returns>
    public static ValueScope For(Locale locale, ChannelId channelId)
    {
        ArgumentNullException.ThrowIfNull(locale);

        return new(locale, channelId);
    }
}
