using Sortiment.Core.Attributes;
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

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, true, true, true)]
    public void ForAttributeKeepsOnlyThePartsTheAttributeVariesBy(
        bool isLocalizable,
        bool isScopable,
        bool expectLocale,
        bool expectChannel)
    {
        var attribute = AttributeDefinition.Create(
                AttributeCode.Parse("description"),
                AttributeType.Text,
                isLocalizable,
                isScopable)
            .Value;

        ValueScope.ForAttribute(attribute, Swedish, Web).ShouldBe(
            new ValueScope(expectLocale ? Swedish : null, expectChannel ? Web : null));
    }
}
