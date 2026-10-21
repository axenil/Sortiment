using Sortiment.Core.Channels;

namespace Sortiment.Core.Tests.Channels;

public sealed class ChannelTests
{
    private static readonly ChannelCode Ecommerce = ChannelCode.Parse("ecommerce");
    private static readonly Locale Swedish = Locale.Parse("sv-SE");
    private static readonly Locale English = Locale.Parse("en-GB");
    private static readonly Locale Finnish = Locale.Parse("fi-FI");

    [Fact]
    public void CreateReturnsAChannelWithItsLocalesInOrder()
    {
        var channel = Channel.Create(Ecommerce, [Swedish, English]).Value;

        channel.Code.ShouldBe(Ecommerce);
        channel.Locales.ShouldBe([Swedish, English]);
    }

    [Fact]
    public void CreateRequiresAtLeastOneLocale()
    {
        var result = Channel.Create(Ecommerce, []);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("channel.locale_required");
        error.Context[ErrorContextKeys.Channel].ShouldBe("ecommerce");
    }

    [Fact]
    public void CreateReportsEachDuplicateLocale()
    {
        var result = Channel.Create(Ecommerce, [Swedish, English, Locale.Parse("sv-SE"), English]);

        result.Errors.Select(error => error.Context[ErrorContextKeys.Locale]).ShouldBe(["sv-SE", "en-GB"]);
        result.Errors.ShouldAllBe(error => error.Code == "channel.locale_already_active");
    }

    [Fact]
    public void HasLocaleTellsWhetherALocaleIsActive()
    {
        var channel = Channel.Create(Ecommerce, [Swedish]).Value;

        channel.HasLocale(Swedish).ShouldBeTrue();
        channel.HasLocale(English).ShouldBeFalse();
    }

    [Fact]
    public void AddLocaleAppendsTheLocale()
    {
        var channel = Channel.Create(Ecommerce, [Swedish]).Value;

        channel.AddLocale(Finnish).IsSuccess.ShouldBeTrue();

        channel.Locales.ShouldBe([Swedish, Finnish]);
    }

    [Fact]
    public void AddLocaleRejectsAnActiveLocale()
    {
        var channel = Channel.Create(Ecommerce, [Swedish]).Value;

        channel.AddLocale(Swedish).Errors.ShouldHaveSingleItem().Code.ShouldBe("channel.locale_already_active");
        channel.Locales.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveLocaleRemovesTheLocale()
    {
        var channel = Channel.Create(Ecommerce, [Swedish, English]).Value;

        channel.RemoveLocale(Swedish).IsSuccess.ShouldBeTrue();

        channel.Locales.ShouldBe([English]);
    }

    [Fact]
    public void RemoveLocaleReportsAnInactiveLocale()
    {
        var channel = Channel.Create(Ecommerce, [Swedish]).Value;

        channel.RemoveLocale(English).Errors.ShouldHaveSingleItem().Code.ShouldBe("channel.locale_not_active");
    }

    [Fact]
    public void RemoveLocaleKeepsTheLastLocale()
    {
        var channel = Channel.Create(Ecommerce, [Swedish]).Value;

        channel.RemoveLocale(Swedish).Errors.ShouldHaveSingleItem().Code.ShouldBe("channel.locale_required");
        channel.Locales.ShouldBe([Swedish]);
    }
}
