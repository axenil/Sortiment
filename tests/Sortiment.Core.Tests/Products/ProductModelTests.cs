using Sortiment.Core.Attributes;
using Sortiment.Core.Families;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests.Products;

public sealed class ProductModelTests
{
    private static readonly ProductModelCode LinenShirt = ProductModelCode.Parse("linen_shirt");

    private readonly TShirtCatalog _catalog = new();
    private readonly VariantSchema _schema;
    private readonly ProductModel _model;

    public ProductModelTests()
    {
        _schema = _catalog.ColorAndSizeSchema();
        _model = ProductModel.Create(LinenShirt, _schema).Value;
    }

    [Fact]
    public void CreateReturnsAModelOfTheFamilyVariantWithoutValues()
    {
        _model.Id.Value.ShouldNotBe(Guid.Empty);
        _model.Code.ShouldBe(LinenShirt);
        _model.FamilyVariantId.ShouldBe(_catalog.ByColorAndSize.Id);
        _model.Values.ShouldBeEmpty();
    }

    [Fact]
    public void SetValueStoresModelAttributes()
    {
        _model.SetValue(_schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"))
            .ShouldSucceed();

        _model.GetValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBe(new TextValue("Linneskjorta"));
        _model.Values.ShouldHaveSingleItem().Attribute.ShouldBe(_catalog.Name.Id);
    }

    [Theory]
    [InlineData("color")]
    [InlineData("gtin")]
    public void SetValueRejectsVariantAttributes(string attribute)
    {
        var value = attribute == "color"
            ? (AttributeValue)new SingleSelectValue(_catalog.Red.Id)
            : new TextValue("07350053850019");

        var error = _model.SetValue(_schema, Code(attribute), ValueScope.Global, value).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("product_model.attribute_not_on_model");
        error.Context[ErrorContextKeys.ProductModel].ShouldBe("linen_shirt");
        error.Context[ErrorContextKeys.FamilyVariant].ShouldBe("by_color_and_size");
        error.Context[ErrorContextKeys.Attribute].ShouldBe(attribute);
        _model.Values.ShouldBeEmpty();
    }

    [Fact]
    public void SetValueRejectsAnAttributeOutsideTheFamily()
    {
        var error = _model.SetValue(_schema, Code("warranty_years"), ValueScope.Global, new NumberValue(2m)).Errors
            .ShouldHaveSingleItem();

        error.Code.ShouldBe("product_model.attribute_not_in_family");
        error.Context[ErrorContextKeys.Family].ShouldBe("t_shirts");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("warranty_years");
    }

    [Fact]
    public void SetValueReportsValidationErrorsWithTheModelCode()
    {
        var result = _model.SetValue(_schema, Code("name"), ValueScope.Global, new TextValue(new string('a', 151)));

        result.Errors.Select(error => error.Code).ShouldBe(["value.locale_required", "value.too_long"]);
        result.Errors.ShouldAllBe(error => error.Context[ErrorContextKeys.ProductModel] == "linen_shirt");
    }

    [Fact]
    public void RemoveValueClearsTheValueOnlyInThatScope()
    {
        _model.SetValue(_schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta")).ShouldSucceed();
        _model.SetValue(_schema, Code("name"), ValueScope.ForLocale(English), new TextValue("Linen shirt")).ShouldSucceed();

        _model.RemoveValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBeTrue();
        _model.RemoveValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBeFalse();

        _model.GetValue(_catalog.Name.Id, ValueScope.ForLocale(English)).ShouldNotBeNull();
    }

    [Fact]
    public void OperationsRequireTheSchemaOfTheModelsFamilyVariant()
    {
        var other = FamilyVariant.Create(
                FamilyVariantCode.Parse("by_size"),
                _catalog.TShirtSchema(),
                [_catalog.Size.Code],
                [])
            .Value;
        var otherSchema = new VariantSchema(_catalog.TShirtSchema(), other);

        Should.Throw<ArgumentException>(() =>
            _model.SetValue(otherSchema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Skjorta")));
        Should.Throw<ArgumentException>(() => _model.Validate(otherSchema));
    }

    [Fact]
    public void ValidateAcceptsAModelWithValidValues()
    {
        _model.SetValue(_schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta")).ShouldSucceed();

        _model.Validate(_schema).ShouldSucceed();
    }

    [Fact]
    public void ValidateReportsValuesAtTheWrongLevelOrOutsideTheFamily()
    {
        _model.SetValue(_schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta")).ShouldSucceed();
        _model.SetValue(_schema, Code("description"), ValueScope.For(Swedish, _catalog.Ecommerce.Id), new TextValue("Skön."))
            .ShouldSucceed();

        _catalog.ByColorAndSize.AddVariantAttribute(_catalog.TShirtSchema(), Code("name")).ShouldSucceed();
        _catalog.TShirts.RemoveAttribute(_catalog.Description.Id).ShouldSucceed();

        var errors = _model.Validate(_schema).Errors;

        errors.Select(error => (error.Code, error.Context[ErrorContextKeys.AttributeId])).ShouldBe(
        [
            ("product_model.attribute_not_on_model", _catalog.Name.Id.ToString()),
            ("product_model.attribute_not_in_family", _catalog.Description.Id.ToString()),
        ]);
    }

    [Fact]
    public void ValidateReportsValuesThatBreakATightenedConstraint()
    {
        _model.SetValue(_schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta")).ShouldSucceed();

        _catalog.Name.SettingsAs<TextSettings>().SetMaxLength(5).ShouldSucceed();

        var error = _model.Validate(_schema).Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("value.too_long");
        error.Context[ErrorContextKeys.ProductModel].ShouldBe("linen_shirt");
    }

    [Fact]
    public void RestoreKeepsTheStoredState()
    {
        var id = ProductModelId.New();
        ProductValue[] values = [new(_catalog.Name.Id, ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"))];

        var model = ProductModel.Restore(id, LinenShirt, _catalog.ByColorAndSize.Id, values);

        model.Id.ShouldBe(id);
        model.Code.ShouldBe(LinenShirt);
        model.FamilyVariantId.ShouldBe(_catalog.ByColorAndSize.Id);
        model.Values.ShouldBe(values);
    }

    [Fact]
    public void RestoreReportsEveryDuplicateValue()
    {
        var exception = Should.Throw<ArgumentException>(() => ProductModel.Restore(
            ProductModelId.New(),
            LinenShirt,
            _catalog.ByColorAndSize.Id,
            [
                new(_catalog.Name.Id, ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta")),
                new(_catalog.Name.Id, ValueScope.ForLocale(Swedish), new TextValue("Skjorta")),
            ]));

        exception.Message.ShouldStartWith("Product model 'linen_shirt' cannot be restored.");
        exception.Message.ShouldContain($"Attribute {_catalog.Name.Id} has more than one value");
    }

    [Fact]
    public void CodesAndIdsFollowTheUsualRules()
    {
        ProductModelCode.Create("Linen_Shirt").Errors.ShouldHaveSingleItem().Code.ShouldBe("code.invalid_characters");
        ProductModelId.New().ShouldNotBe(ProductModelId.New());
        Should.Throw<ArgumentException>(() => new ProductModelId(Guid.Empty));
    }

    private static AttributeCode Code(string value) => AttributeCode.Parse(value);
}
