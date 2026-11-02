using Sortiment.Core.Attributes;
using Sortiment.Core.Families;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests.Products;

public sealed class VariantProductTests
{
    private readonly TShirtCatalog _catalog = new();
    private readonly VariantContext _context;
    private readonly Product _variant;

    public VariantProductTests()
    {
        var schema = _catalog.ColorAndSizeSchema();
        var model = ProductModel.Create(ProductModelCode.Parse("linen_shirt"), schema).Value;
        _context = new VariantContext(schema, model);
        _variant = Product.CreateVariant(Sku.Parse("TS-001-RED-M"), _context).Value;
    }

    private ProductModel Model => _context.Model;

    [Fact]
    public void CreateVariantReturnsAnEnabledVariantOfTheModelInItsFamily()
    {
        _variant.ProductModelId.ShouldBe(Model.Id);
        _variant.IsVariant.ShouldBeTrue();
        _variant.FamilyId.ShouldBe(_catalog.TShirts.Id);
        _variant.IsEnabled.ShouldBeTrue();
        _variant.Values.ShouldBeEmpty();
    }

    [Fact]
    public void ProductsWithoutAModelAreNotVariants()
    {
        var product = Product.Create(Sku.Parse("TS-002"), _catalog.TShirtSchema()).Value;

        product.ProductModelId.ShouldBeNull();
        product.IsVariant.ShouldBeFalse();
    }

    [Fact]
    public void AContextNeedsAModelOfItsFamilyVariant()
    {
        var bySize = FamilyVariant.Create(FamilyVariantCode.Parse("by_size"), _catalog.TShirtSchema(), [_catalog.Size.Code], [])
            .Value;

        Should.Throw<ArgumentException>(() =>
            new VariantContext(new VariantSchema(_catalog.TShirtSchema(), bySize), Model));
    }

    [Fact]
    public void SetValueStoresVariantAttributes()
    {
        SetAxes();
        _variant.SetValue(_context, Code("gtin"), ValueScope.Global, new TextValue("07350053850019")).ShouldSucceed();

        _variant.Values.Select(value => value.Attribute)
            .ShouldBe([_catalog.Color.Id, _catalog.Size.Id, _catalog.Gtin.Id], ignoreOrder: true);
    }

    [Fact]
    public void SetValueRejectsModelAttributes()
    {
        var error = _variant.SetValue(_context, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Skjorta"))
            .Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("product.attribute_on_model");
        error.Context[ErrorContextKeys.Product].ShouldBe("TS-001-RED-M");
        error.Context[ErrorContextKeys.FamilyVariant].ShouldBe("by_color_and_size");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("name");
        _variant.Values.ShouldBeEmpty();
    }

    [Fact]
    public void SetValueRejectsAnAttributeOutsideTheFamily()
    {
        _variant.SetValue(_context, Code("warranty_years"), ValueScope.Global, new NumberValue(2m)).Errors
            .ShouldHaveSingleItem().Code.ShouldBe("product.attribute_not_in_family");
    }

    [Fact]
    public void SetValueValidatesAxisValues()
    {
        _variant.SetValue(_context, Code("size"), ValueScope.Global, new SingleSelectValue(_catalog.Red.Id)).Errors
            .ShouldHaveSingleItem().Code.ShouldBe("value.unknown_option");
    }

    [Fact]
    public void FamilySchemaOperationsAreRefusedForVariants()
    {
        var schema = _catalog.TShirtSchema();

        Should.Throw<ArgumentException>(() =>
            _variant.SetValue(schema, Code("gtin"), ValueScope.Global, new TextValue("07350053850019")));
        Should.Throw<ArgumentException>(() => _variant.Validate(schema));
        Should.Throw<ArgumentException>(() => _variant.CalculateCompleteness(schema, _catalog.Ecommerce));
    }

    [Fact]
    public void VariantOperationsRequireTheVariantsOwnModel()
    {
        var otherModel = ProductModel.Create(ProductModelCode.Parse("cotton_shirt"), _catalog.ColorAndSizeSchema()).Value;
        var otherContext = new VariantContext(_catalog.ColorAndSizeSchema(), otherModel);

        Should.Throw<ArgumentException>(() =>
            _variant.SetValue(otherContext, Code("gtin"), ValueScope.Global, new TextValue("07350053850019")));
        Should.Throw<ArgumentException>(() => _variant.Validate(otherContext));
        Should.Throw<ArgumentException>(() => _variant.AxisCombination(otherContext));
        Should.Throw<ArgumentException>(() => _variant.EffectiveValues(otherModel));
    }

    [Fact]
    public void ChangeFamilyIsNotAllowedForVariants()
    {
        Should.Throw<InvalidOperationException>(() => _variant.ChangeFamily(_catalog.AccessorySchema()));
        _variant.FamilyId.ShouldBe(_catalog.TShirts.Id);
    }

    [Fact]
    public void ValidateReportsEveryAxisWithoutAValue()
    {
        var errors = _variant.Validate(_context).Errors;

        errors.Select(error => (error.Code, error.Context[ErrorContextKeys.Attribute])).ShouldBe(
        [
            ("product.axis_value_missing", "color"),
            ("product.axis_value_missing", "size"),
        ]);
    }

    [Fact]
    public void ValidateAcceptsAVariantWithEveryAxisValue()
    {
        SetAxes();

        _variant.Validate(_context).ShouldSucceed();
    }

    [Fact]
    public void ValidateReportsValuesThatBecameModelAttributes()
    {
        SetAxes();
        _variant.SetValue(_context, Code("gtin"), ValueScope.Global, new TextValue("07350053850019")).ShouldSucceed();

        _catalog.ByColorAndSize.RemoveVariantAttribute(_catalog.Gtin.Id).ShouldSucceed();

        var error = _variant.Validate(_context).Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("product.attribute_on_model");
        error.Context[ErrorContextKeys.AttributeId].ShouldBe(_catalog.Gtin.Id.ToString());
    }

    [Fact]
    public void EffectiveValuesCombineTheVariantAndItsModel()
    {
        SetAxes();
        Model.SetValue(_context.Schema, Code("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"))
            .ShouldSucceed();

        _variant.EffectiveValues(Model).Select(value => value.Attribute)
            .ShouldBe([_catalog.Color.Id, _catalog.Size.Id, _catalog.Name.Id], ignoreOrder: true);
        _variant.GetEffectiveValue(Model, _catalog.Name.Id, ValueScope.ForLocale(Swedish))
            .ShouldBe(new TextValue("Linneskjorta"));
        _variant.GetEffectiveValue(Model, _catalog.Color.Id, ValueScope.Global)
            .ShouldBe(new SingleSelectValue(_catalog.Red.Id));
        _variant.GetEffectiveValue(Model, _catalog.Name.Id, ValueScope.ForLocale(English)).ShouldBeNull();
        _variant.GetValue(_catalog.Name.Id, ValueScope.ForLocale(Swedish)).ShouldBeNull();
    }

    [Fact]
    public void TheAxisCombinationNeedsEveryAxis()
    {
        _variant.SetValue(_context, Code("color"), ValueScope.Global, new SingleSelectValue(_catalog.Red.Id))
            .ShouldSucceed();

        _variant.AxisCombination(_context).ShouldBeNull();
    }

    [Fact]
    public void VariantsWithTheSameAxisValuesHaveEqualCombinations()
    {
        SetAxes();
        var sibling = Product.CreateVariant(Sku.Parse("TS-001-RED-M-2"), _context).Value;
        SetAxes(sibling);
        var other = Product.CreateVariant(Sku.Parse("TS-001-BLUE-M"), _context).Value;
        SetAxes(other, _catalog.Blue);

        var combination = _variant.AxisCombination(_context)!;

        combination.Values.Select(value => value.Attribute).ShouldBe([_catalog.Color.Id, _catalog.Size.Id]);
        combination.ShouldBe(sibling.AxisCombination(_context));
        combination.Key.ShouldBe(sibling.AxisCombination(_context)!.Key);
        combination.GetHashCode().ShouldBe(sibling.AxisCombination(_context)!.GetHashCode());
        combination.ShouldNotBe(other.AxisCombination(_context));
        combination.Key.ShouldBe($"{_catalog.Color.Id}={_catalog.Red.Id};{_catalog.Size.Id}={_catalog.Medium.Id}");
    }

    [Fact]
    public void BooleanAxesHaveStableKeys()
    {
        var organic = AttributeDefinition.Create(AttributeCode.Parse("organic"), AttributeType.Boolean).Value;
        _catalog.TShirts.AddAttribute(organic).ShouldSucceed();
        var familySchema = FamilySchema.Create(_catalog.TShirts, [.. _catalog.AllAttributes, organic]).Value;
        var byOrganic = FamilyVariant.Create(FamilyVariantCode.Parse("by_organic"), familySchema, [organic.Code], []).Value;
        var schema = new VariantSchema(familySchema, byOrganic);
        var context = new VariantContext(schema, ProductModel.Create(ProductModelCode.Parse("basic_tee"), schema).Value);
        var variant = Product.CreateVariant(Sku.Parse("TEE-ORGANIC"), context).Value;

        variant.SetValue(context, organic.Code, ValueScope.Global, new BooleanValue(true)).ShouldSucceed();

        variant.AxisCombination(context)!.Key.ShouldBe($"{organic.Id}=true");
    }

    [Fact]
    public void RestoreKeepsTheModel()
    {
        var id = ProductId.New();

        var variant = Product.Restore(
            id,
            Sku.Parse("TS-001-RED-M"),
            _catalog.TShirts.Id,
            isEnabled: true,
            [new(_catalog.Color.Id, ValueScope.Global, new SingleSelectValue(_catalog.Red.Id))],
            Model.Id);

        variant.Id.ShouldBe(id);
        variant.ProductModelId.ShouldBe(Model.Id);
        variant.IsVariant.ShouldBeTrue();
    }

    private void SetAxes() => SetAxes(_variant);

    private void SetAxes(Product variant, AttributeOption? color = null)
    {
        variant.SetValue(_context, Code("color"), ValueScope.Global, new SingleSelectValue((color ?? _catalog.Red).Id))
            .ShouldSucceed();
        variant.SetValue(_context, Code("size"), ValueScope.Global, new SingleSelectValue(_catalog.Medium.Id))
            .ShouldSucceed();
    }

    private static AttributeCode Code(string value) => AttributeCode.Parse(value);
}
