using Sortiment.Core.Attributes;
using Sortiment.Core.Families;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests.Products;

public sealed class CompletenessTests
{
    private readonly TShirtCatalog _catalog = new();
    private readonly FamilySchema _schema;
    private readonly Product _product;

    public CompletenessTests()
    {
        _schema = _catalog.TShirtSchema();
        _product = Product.Create(Sku.Parse("TS-001"), _schema).Value;
    }

    [Fact]
    public void AProductWithoutValuesMissesEveryRequiredAttributeInEveryLocale()
    {
        var completeness = _product.CalculateCompleteness(_schema, _catalog.Ecommerce);

        completeness.ChannelId.ShouldBe(_catalog.Ecommerce.Id);
        completeness.IsComplete.ShouldBeFalse();
        completeness.Locales.Select(locale => locale.Locale).ShouldBe([Swedish, English]);
        completeness.Locales.ShouldAllBe(locale => locale.RequiredCount == 3 && locale.FilledCount == 0);
        completeness.ForLocale(Swedish)!.MissingAttributes.ShouldBe(Codes("name", "description", "color"));
        completeness.ForLocale(Swedish)!.Percentage.ShouldBe(0);
    }

    [Fact]
    public void ALocalizableValueCountsOnlyForItsLocale()
    {
        SetValue("name", ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"));

        var completeness = _product.CalculateCompleteness(_schema, _catalog.Ecommerce);

        completeness.ForLocale(Swedish)!.MissingAttributes.ShouldBe(Codes("description", "color"));
        completeness.ForLocale(English)!.MissingAttributes.ShouldBe(Codes("name", "description", "color"));
    }

    [Fact]
    public void AGlobalValueCountsForEveryLocale()
    {
        SetValue("color", ValueScope.Global, new SingleSelectValue(_catalog.Red.Id));

        var completeness = _product.CalculateCompleteness(_schema, _catalog.Ecommerce);

        completeness.Locales.ShouldAllBe(locale => !locale.MissingAttributes.Contains(Code("color")));
    }

    [Fact]
    public void AScopableValueCountsOnlyForItsChannel()
    {
        SetValue(
            "description",
            ValueScope.For(Swedish, _catalog.Print.Id),
            new TextValue("Skjorta i linne för tryck."));

        var completeness = _product.CalculateCompleteness(_schema, _catalog.Ecommerce);

        completeness.ForLocale(Swedish)!.MissingAttributes.ShouldContain(Code("description"));
    }

    [Fact]
    public void AProductWithEveryRequiredValueIsComplete()
    {
        FillEcommerceRequirements();

        var completeness = _product.CalculateCompleteness(_schema, _catalog.Ecommerce);

        completeness.IsComplete.ShouldBeTrue();
        completeness.Locales.ShouldAllBe(locale => locale.IsComplete && locale.Percentage == 100);
    }

    [Fact]
    public void CompletenessIsCalculatedAgainstEachChannelsOwnRequirementsAndLocales()
    {
        SetValue("name", ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"));

        var ecommerce = _product.CalculateCompleteness(_schema, _catalog.Ecommerce);
        var print = _product.CalculateCompleteness(_schema, _catalog.Print);

        ecommerce.IsComplete.ShouldBeFalse();
        print.IsComplete.ShouldBeTrue();
        print.Locales.ShouldHaveSingleItem().RequiredCount.ShouldBe(1);
    }

    [Fact]
    public void PercentageIsRoundedDown()
    {
        FillEcommerceRequirements();
        _product.RemoveValue(_catalog.Color.Id, ValueScope.Global);

        _product.CalculateCompleteness(_schema, _catalog.Ecommerce).ForLocale(Swedish)!.Percentage.ShouldBe(66);
    }

    [Fact]
    public void AChannelWithoutRequirementsIsComplete()
    {
        var accessory = Product.Create(Sku.Parse("AC-001"), _catalog.AccessorySchema()).Value;

        var completeness = accessory.CalculateCompleteness(_catalog.AccessorySchema(), _catalog.Ecommerce);

        completeness.IsComplete.ShouldBeTrue();
        completeness.Locales.ShouldAllBe(locale => locale.RequiredCount == 0 && locale.Percentage == 100);
    }

    [Fact]
    public void InvalidValuesStillCountAsFilled()
    {
        FillEcommerceRequirements();
        _catalog.Name.SettingsAs<TextSettings>().SetMaxLength(3);

        _product.Validate(_schema).IsFailure.ShouldBeTrue();
        _product.CalculateCompleteness(_schema, _catalog.Ecommerce).IsComplete.ShouldBeTrue();
    }

    [Fact]
    public void CompletenessFollowsChangesToTheFamilyAndChannel()
    {
        FillEcommerceRequirements();

        _catalog.TShirts.RequireAttribute(_catalog.Weight, _catalog.Ecommerce.Id);
        _catalog.Ecommerce.AddLocale(Locale.Parse("fi-FI"));

        var completeness = _product.CalculateCompleteness(_schema, _catalog.Ecommerce);
        completeness.ForLocale(Swedish)!.MissingAttributes.ShouldBe(Codes("weight"));
        completeness.ForLocale(Locale.Parse("fi-FI"))!.MissingAttributes
            .ShouldBe(Codes("name", "description", "weight"));
    }

    [Fact]
    public void ForLocaleReturnsNullForALocaleNotActiveInTheChannel()
    {
        _product.CalculateCompleteness(_schema, _catalog.Print).ForLocale(English).ShouldBeNull();
    }

    [Fact]
    public void CalculateCompletenessRequiresTheSchemaOfTheProductsFamily()
    {
        Should.Throw<ArgumentException>(
            () => _product.CalculateCompleteness(_catalog.AccessorySchema(), _catalog.Ecommerce));
    }

    private void FillEcommerceRequirements()
    {
        SetValue("color", ValueScope.Global, new SingleSelectValue(_catalog.Blue.Id));
        foreach (var locale in _catalog.Ecommerce.Locales)
        {
            SetValue("name", ValueScope.ForLocale(locale), new TextValue($"Linen shirt {locale}"));
            SetValue("description", ValueScope.For(locale, _catalog.Ecommerce.Id), new TextValue("Linen."));
        }
    }

    private void SetValue(string attribute, ValueScope scope, AttributeValue value) =>
        _product.SetValue(_schema, Code(attribute), scope, value).IsSuccess.ShouldBeTrue();

    private static AttributeCode Code(string value) => AttributeCode.Parse(value);

    private static AttributeCode[] Codes(params string[] values) => values.Select(Code).ToArray();
}
