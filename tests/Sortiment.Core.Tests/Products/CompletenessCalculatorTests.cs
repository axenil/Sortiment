using Sortiment.Core.Attributes;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests.Products;

public sealed class CompletenessCalculatorTests
{
    private readonly TShirtCatalog _catalog = new();

    [Fact]
    public void AValueCountsAsFilledWhicheverSourceItComesFrom()
    {
        var channel = _catalog.Ecommerce;
        var required = _catalog.TShirtSchema().RequiredAttributes(channel.Id);
        var shared = new HashSet<(AttributeId, ValueScope)>
        {
            (_catalog.Name.Id, ValueScope.ForLocale(Swedish)),
            (_catalog.Description.Id, ValueScope.For(Swedish, channel.Id)),
        };
        var own = new HashSet<(AttributeId, ValueScope)> { (_catalog.Color.Id, ValueScope.Global) };

        var completeness = CompletenessCalculator.Calculate(
            required,
            channel,
            (attribute, scope) => shared.Contains((attribute, scope)) || own.Contains((attribute, scope)));

        completeness.ForLocale(Swedish)!.IsComplete.ShouldBeTrue();
        completeness.ForLocale(English)!.MissingAttributes.Select(code => code.Value).ShouldBe(["name", "description"]);
    }
}
