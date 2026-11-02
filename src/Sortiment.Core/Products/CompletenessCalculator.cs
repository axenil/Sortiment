using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Values;

namespace Sortiment.Core.Products;

/// <summary>
/// Calculates completeness from required attributes and a way to tell whether a value exists, so the same rules apply
/// wherever the values come from (ADR 0010).
/// </summary>
internal static class CompletenessCalculator
{
    /// <param name="required">The attributes the channel requires.</param>
    /// <param name="channel">The channel.</param>
    /// <param name="hasValue">Whether there is a value for an attribute in a scope.</param>
    public static ChannelCompleteness Calculate(
        IReadOnlyList<AttributeDefinition> required,
        Channel channel,
        Func<AttributeId, ValueScope, bool> hasValue)
    {
        var locales = channel.Locales
            .Select(locale => ForLocale(required, locale, channel.Id, hasValue))
            .ToList();

        return new ChannelCompleteness(channel.Id, locales);
    }

    private static LocaleCompleteness ForLocale(
        IReadOnlyList<AttributeDefinition> required,
        Locale locale,
        ChannelId channel,
        Func<AttributeId, ValueScope, bool> hasValue)
    {
        var missing = required
            .Where(attribute => !hasValue(attribute.Id, ValueScope.ForAttribute(attribute, locale, channel)))
            .Select(attribute => attribute.Code)
            .ToList();

        return new LocaleCompleteness(locale, required.Count, missing);
    }
}
