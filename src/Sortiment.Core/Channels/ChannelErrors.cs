namespace Sortiment.Core.Channels;

internal static class ChannelErrors
{
    public static ResultError LocaleRequired(ChannelCode channel) =>
        new ResultError(
                "channel.locale_required",
                $"Channel '{channel}' must have at least one locale.")
            .WithContext(ErrorContextKeys.Channel, channel.Value);

    public static ResultError LocaleAlreadyActive(ChannelCode channel, Locale locale) =>
        new ResultError(
                "channel.locale_already_active",
                $"Locale '{locale}' is already active in channel '{channel}'.")
            .WithContext(ErrorContextKeys.Channel, channel.Value)
            .WithContext(ErrorContextKeys.Locale, locale.Code);

    public static ResultError LocaleNotActive(ChannelCode channel, Locale locale) =>
        new ResultError(
                "channel.locale_not_active",
                $"Locale '{locale}' is not active in channel '{channel}'.")
            .WithContext(ErrorContextKeys.Channel, channel.Value)
            .WithContext(ErrorContextKeys.Locale, locale.Code);
}
