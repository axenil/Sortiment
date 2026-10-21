using Sortiment.Core.Attributes;
using Sortiment.Core.Measurements;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests.Products;

public sealed class ProductTests
{
    private readonly TShirtCatalog _catalog = new();
    private readonly Sku _sku = Sku.Parse("TS-001");

    [Fact]
    public void CreateReturnsAnEnabledProductInTheFamilyWithoutValues()
    {
        var product = Product.Create(_sku, _catalog.TShirtSchema()).Value;

        product.Sku.ShouldBe(_sku);
        product.FamilyId.ShouldBe(_catalog.TShirts.Id);
        product.IsEnabled.ShouldBeTrue();
        product.Values.ShouldBeEmpty();
    }

    [Fact]
    public void ProductsCanBeDisabledAndEnabled()
    {
        var product = NewProduct();

        product.Disable();
        product.IsEnabled.ShouldBeFalse();

        product.Enable();
        product.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void ChangeSkuCanCorrectTheCasing()
    {
        var product = NewProduct();

        product.ChangeSku(Sku.Parse("ts-001"));

        product.Sku.Value.ShouldBe("ts-001");
    }

    [Fact]
    public void SetValueStoresTheValueInItsScope()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();

        product.SetValue(schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta")).IsSuccess
            .ShouldBeTrue();
        product.SetValue(schema, Code("name"), ValueScope.ForLocale(English), new TextValue("Linen shirt")).IsSuccess
            .ShouldBeTrue();

        product.GetValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBe(new TextValue("Linneskjorta"));
        product.GetValue(_catalog.Name.Id, ValueScope.ForLocale(English)).ShouldBe(new TextValue("Linen shirt"));
        product.GetValue(_catalog.Name.Id, ValueScope.ForLocale(Locale.Parse("fi-FI"))).ShouldBeNull();
        product.Values.Count.ShouldBe(2);
    }

    [Fact]
    public void SetValueReplacesTheValueInTheSameScope()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();

        product.SetValue(schema, Code("color"), ValueScope.Global, new SingleSelectValue(_catalog.Red.Id));
        product.SetValue(schema, Code("color"), ValueScope.Global, new SingleSelectValue(_catalog.Blue.Id));

        product.Values.ShouldHaveSingleItem().ShouldBe(
            new ProductValue(_catalog.Color.Id, ValueScope.Global, new SingleSelectValue(_catalog.Blue.Id)));
    }

    [Fact]
    public void SetValueRejectsAnAttributeOutsideTheFamily()
    {
        var product = NewProduct();

        var error = product.SetValue(
                _catalog.TShirtSchema(),
                Code("warranty_years"),
                ValueScope.Global,
                new NumberValue(2m))
            .Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("product.attribute_not_in_family");
        error.Context[ErrorContextKeys.Product].ShouldBe("TS-001");
        error.Context[ErrorContextKeys.Family].ShouldBe("t_shirts");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("warranty_years");
        product.Values.ShouldBeEmpty();
    }

    [Fact]
    public void SetValueReportsEveryValidationErrorWithTheSku()
    {
        var product = NewProduct();

        var result = product.SetValue(
            _catalog.TShirtSchema(),
            Code("name"),
            ValueScope.Global,
            new TextValue(new string('a', 151)));

        result.Errors.Select(error => error.Code).ShouldBe(["value.locale_required", "value.too_long"]);
        result.Errors.ShouldAllBe(error => error.Context[ErrorContextKeys.Product] == "TS-001");
        product.Values.ShouldBeEmpty();
    }

    [Fact]
    public void AnInvalidValueDoesNotReplaceTheCurrentOne()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();
        product.SetValue(schema, Code("weight"), ValueScope.Global, new MeasurementValue(180m, UnitCode.Parse("g")));

        product.SetValue(schema, Code("weight"), ValueScope.Global, new MeasurementValue(1m, UnitCode.Parse("cm")));

        product.GetValue(_catalog.Weight.Id, ValueScope.Global)
            .ShouldBe(new MeasurementValue(180m, UnitCode.Parse("g")));
    }

    [Fact]
    public void SetValueRequiresTheSchemaOfTheProductsFamily()
    {
        var product = NewProduct();

        Should.Throw<ArgumentException>(() => product.SetValue(
            _catalog.AccessorySchema(),
            Code("name"),
            ValueScope.ForLocale(Swedish),
            new TextValue("Skjorta")));
    }

    [Fact]
    public void RemoveValueClearsTheValueOnlyInThatScope()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();
        product.SetValue(schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"));
        product.SetValue(schema, Code("name"), ValueScope.ForLocale(English), new TextValue("Linen shirt"));

        product.RemoveValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBeTrue();
        product.RemoveValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBeFalse();

        product.GetValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBeNull();
        product.GetValue(_catalog.Name.Id, ValueScope.ForLocale(English)).ShouldNotBeNull();
    }

    [Fact]
    public void ChangeFamilyKeepsSharedValuesAndReturnsTheRemovedOnes()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();
        product.SetValue(schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"));
        product.SetValue(schema, Code("color"), ValueScope.Global, new SingleSelectValue(_catalog.Red.Id));

        var removed = product.ChangeFamily(_catalog.AccessorySchema());

        product.FamilyId.ShouldBe(_catalog.Accessories.Id);
        removed.ShouldHaveSingleItem().Attribute.ShouldBe(_catalog.Color.Id);
        product.Values.ShouldHaveSingleItem().Attribute.ShouldBe(_catalog.Name.Id);
    }

    [Fact]
    public void ValidateAcceptsAProductWithValidValues()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();
        product.SetValue(schema, Code("gtin"), ValueScope.Global, new TextValue("07350053850019"));

        product.Validate(schema).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ValidateReportsValuesThatBreakATightenedConstraint()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();
        product.SetValue(schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"));

        _catalog.Name.SettingsAs<TextSettings>().SetMaxLength(5);

        var error = product.Validate(schema).Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("value.too_long");
        error.Context[ErrorContextKeys.Product].ShouldBe("TS-001");
        error.Context[ErrorContextKeys.Locale].ShouldBe("sv-SE");
    }

    [Fact]
    public void ValidateReportsValuesOfAttributesRemovedFromTheFamily()
    {
        var product = NewProduct();
        var schema = _catalog.TShirtSchema();
        product.SetValue(schema, Code("color"), ValueScope.Global, new SingleSelectValue(_catalog.Red.Id));

        _catalog.TShirts.RemoveAttribute(_catalog.Color.Id);

        product.Validate(schema).Errors.ShouldHaveSingleItem().Code.ShouldBe("product.attribute_not_in_family");
    }

    private Product NewProduct() => Product.Create(_sku, _catalog.TShirtSchema()).Value;

    private static AttributeCode Code(string value) => AttributeCode.Parse(value);
}
