using Sortiment.Core.Channels;
using Sortiment.Core.Values;

namespace Sortiment.Core.Tests.Values;

public sealed class ValueScopeTests
{
    private static readonly Locale Swedish = Locale.Parse("sv-SE");
    private static readonly ChannelId Web = ChannelId.New();

    [Fact]
    public void GlobalHasNeitherLocaleNorChannel()
    {
        ValueScope.Global.ShouldBe(new ValueScope(null, null));
    }

    [Fact]
    public void FactoriesSetTheGivenParts()
    {
        ValueScope.ForLocale(Swedish).ShouldBe(new ValueScope(Swedish, null));
        ValueScope.ForChannel(Web).ShouldBe(new ValueScope(null, Web));
        ValueScope.For(Swedish, Web).ShouldBe(new ValueScope(Swedish, Web));
    }
}
