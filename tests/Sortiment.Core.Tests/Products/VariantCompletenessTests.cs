using Sortiment.Core.Attributes;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests.Products;

public sealed class VariantCompletenessTests
{
    private readonly TShirtCatalog _catalog = new();
    private readonly VariantContext _context;

    public VariantCompletenessTests()
    {
        var schema = _catalog.ColorAndSizeSchema();
        _context = new VariantContext(
            schema,
            ProductModel.Create(ProductModelCode.Parse("linen_shirt"), schema).Value);
    }

    [Fact]
    public void AModelValueCountsForEveryVariant()
    {
        var red = Variant("TS-001-RED-M", _catalog.Red);
        var blue = Variant("TS-001-BLUE-M", _catalog.Blue);

        SetModelValue("name", ValueScope.ForLocale(Swedish), "Linneskjorta");
        SetModelValue("description", ValueScope.For(Swedish, _catalog.Ecommerce.Id), "Skön linneskjorta.");

        red.CalculateCompleteness(_context, _catalog.Ecommerce).ForLocale(Swedish)!.IsComplete.ShouldBeTrue();
        blue.CalculateCompleteness(_context, _catalog.Ecommerce).ForLocale(Swedish)!.IsComplete.ShouldBeTrue();
    }

    [Fact]
    public void MissingModelAndVariantValuesAreBothReported()
    {
        var variant = Product.CreateVariant(Sku.Parse("TS-001"), _context).Value;
        SetModelValue("name", ValueScope.ForLocale(Swedish), "Linneskjorta");

        var swedish = variant.CalculateCompleteness(_context, _catalog.Ecommerce).ForLocale(Swedish)!;

        swedish.RequiredCount.ShouldBe(3);
        swedish.MissingAttributes.Select(code => code.Value).ShouldBe(["description", "color"]);
    }

    [Fact]
    public void CompletenessRequiresTheVariantsOwnModel()
    {
        var variant = Variant("TS-001-RED-M", _catalog.Red);
        var otherModel = ProductModel.Create(ProductModelCode.Parse("cotton_shirt"), _context.Schema).Value;

        Should.Throw<ArgumentException>(() =>
            variant.CalculateCompleteness(new VariantContext(_context.Schema, otherModel), _catalog.Ecommerce));
    }

    private Product Variant(string sku, AttributeOption color)
    {
        var variant = Product.CreateVariant(Sku.Parse(sku), _context).Value;
        variant.SetValue(_context, AttributeCode.Parse("color"), ValueScope.Global, new SingleSelectValue(color.Id))
            .ShouldSucceed();
        variant.SetValue(_context, AttributeCode.Parse("size"), ValueScope.Global, new SingleSelectValue(_catalog.Medium.Id))
            .ShouldSucceed();
        return variant;
    }

    private void SetModelValue(string attribute, ValueScope scope, string text) =>
        _context.Model.SetValue(_context.Schema, AttributeCode.Parse(attribute), scope, new TextValue(text))
            .ShouldSucceed();
}
