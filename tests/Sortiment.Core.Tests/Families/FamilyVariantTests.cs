using Sortiment.Core.Attributes;
using Sortiment.Core.Families;
using Sortiment.Core.Tests.Fixtures;

namespace Sortiment.Core.Tests.Families;

public sealed class FamilyVariantTests
{
    private static readonly FamilyVariantCode ByColorAndSize = FamilyVariantCode.Parse("by_color_and_size");

    private readonly TShirtCatalog _catalog = new();

    [Fact]
    public void CreateKeepsTheAxesInOrderAndPutsThemFirstAmongTheVariantAttributes()
    {
        var familyVariant = Create(["color", "size"], ["gtin", "weight"]).Value;

        familyVariant.Id.Value.ShouldNotBe(Guid.Empty);
        familyVariant.Code.ShouldBe(ByColorAndSize);
        familyVariant.FamilyId.ShouldBe(_catalog.TShirts.Id);
        familyVariant.Axes.ShouldBe([_catalog.Color.Id, _catalog.Size.Id]);
        familyVariant.VariantAttributes.ShouldBe([_catalog.Color.Id, _catalog.Size.Id, _catalog.Gtin.Id, _catalog.Weight.Id]);
    }

    [Fact]
    public void CreateIgnoresAxesAndRepeatsAmongTheVariantAttributes()
    {
        var familyVariant = Create(["size"], ["size", "gtin", "gtin"]).Value;

        familyVariant.VariantAttributes.ShouldBe([_catalog.Size.Id, _catalog.Gtin.Id]);
    }

    [Fact]
    public void BooleanAttributesCanBeAxes()
    {
        var organic = AttributeDefinition.Create(AttributeCode.Parse("organic"), AttributeType.Boolean).Value;
        _catalog.TShirts.AddAttribute(organic).ShouldSucceed();
        var schema = FamilySchema.Create(_catalog.TShirts, [.. _catalog.AllAttributes, organic]).Value;

        FamilyVariant.Create(ByColorAndSize, schema, [organic.Code], []).ShouldSucceed();
    }

    [Fact]
    public void CreateRequiresAtLeastOneAxis()
    {
        var error = Create([], ["gtin"]).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("family_variant.axis_required");
        error.Context[ErrorContextKeys.FamilyVariant].ShouldBe("by_color_and_size");
    }

    [Fact]
    public void CreateAllowsAtMostFiveAxes()
    {
        var family = Family.Create(FamilyCode.Parse("posters")).Value;
        var axes = Enumerable.Range(1, FamilyVariant.MaxAxes + 1)
            .Select(number => AttributeDefinition.Create(AttributeCode.Parse($"axis_{number}"), AttributeType.Boolean).Value)
            .ToList();
        foreach (var axis in axes)
        {
            family.AddAttribute(axis).ShouldSucceed();
        }

        var schema = FamilySchema.Create(family, axes).Value;

        FamilyVariant.Create(ByColorAndSize, schema, axes.Take(FamilyVariant.MaxAxes).Select(axis => axis.Code), [])
            .ShouldSucceed();
        FamilyVariant.Create(ByColorAndSize, schema, axes.Select(axis => axis.Code), []).Errors.ShouldHaveSingleItem()
            .Code.ShouldBe("family_variant.too_many_axes");
    }

    [Fact]
    public void CreateReportsEveryInvalidAxisAndAttribute()
    {
        var result = Create(["color", "color", "name", "gtin", "warranty_years"], ["material"]);

        result.Errors.Select(error => (error.Code, error.Context[ErrorContextKeys.Attribute])).ShouldBe(
        [
            ("family_variant.duplicate_axis", "color"),
            ("family_variant.invalid_axis_type", "name"),
            ("family_variant.axis_not_global", "name"),
            ("family_variant.invalid_axis_type", "gtin"),
            ("family_variant.attribute_not_in_family", "warranty_years"),
            ("family_variant.attribute_not_in_family", "material"),
        ]);
    }

    [Fact]
    public void IsAxisAndIsVariantAttributeTellTheLevelsApart()
    {
        var familyVariant = Create(["size"], ["gtin"]).Value;

        familyVariant.IsAxis(_catalog.Size.Id).ShouldBeTrue();
        familyVariant.IsAxis(_catalog.Gtin.Id).ShouldBeFalse();
        familyVariant.IsVariantAttribute(_catalog.Size.Id).ShouldBeTrue();
        familyVariant.IsVariantAttribute(_catalog.Gtin.Id).ShouldBeTrue();
        familyVariant.IsVariantAttribute(_catalog.Name.Id).ShouldBeFalse();
    }

    [Fact]
    public void AddVariantAttributeAddsAFamilyAttributeOnce()
    {
        var familyVariant = Create(["size"], []).Value;

        familyVariant.AddVariantAttribute(_catalog.TShirtSchema(), _catalog.Weight.Code).ShouldSucceed();
        familyVariant.AddVariantAttribute(_catalog.TShirtSchema(), _catalog.Weight.Code).ShouldSucceed();

        familyVariant.VariantAttributes.ShouldBe([_catalog.Size.Id, _catalog.Weight.Id]);
    }

    [Fact]
    public void AddVariantAttributeRejectsAnAttributeOutsideTheFamily()
    {
        var familyVariant = Create(["size"], []).Value;

        familyVariant.AddVariantAttribute(_catalog.TShirtSchema(), _catalog.Warranty.Code).Errors
            .ShouldHaveSingleItem().Code.ShouldBe("family_variant.attribute_not_in_family");
    }

    [Fact]
    public void AddVariantAttributeRequiresTheSchemaOfItsFamily()
    {
        var familyVariant = Create(["size"], []).Value;

        Should.Throw<ArgumentException>(() =>
            familyVariant.AddVariantAttribute(_catalog.AccessorySchema(), _catalog.Name.Code));
    }

    [Fact]
    public void RemoveVariantAttributeMakesItAModelAttributeButKeepsTheAxes()
    {
        var familyVariant = Create(["size"], ["gtin"]).Value;

        familyVariant.RemoveVariantAttribute(_catalog.Gtin.Id).ShouldSucceed();
        familyVariant.RemoveVariantAttribute(_catalog.Name.Id).ShouldSucceed();
        var error = familyVariant.RemoveVariantAttribute(_catalog.Size.Id).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("family_variant.axis_cannot_be_removed");
        error.Context[ErrorContextKeys.AttributeId].ShouldBe(_catalog.Size.Id.ToString());
        familyVariant.VariantAttributes.ShouldBe([_catalog.Size.Id]);
    }

    [Fact]
    public void RestoreKeepsTheStoredState()
    {
        var id = FamilyVariantId.New();

        var familyVariant = FamilyVariant.Restore(
            id,
            ByColorAndSize,
            _catalog.TShirts.Id,
            [_catalog.Color.Id, _catalog.Size.Id],
            [_catalog.Color.Id, _catalog.Size.Id, _catalog.Gtin.Id]);

        familyVariant.Id.ShouldBe(id);
        familyVariant.Axes.ShouldBe([_catalog.Color.Id, _catalog.Size.Id]);
        familyVariant.VariantAttributes.ShouldBe([_catalog.Color.Id, _catalog.Size.Id, _catalog.Gtin.Id]);
    }

    [Fact]
    public void RestoreReportsEveryStructuralProblem()
    {
        var exception = Should.Throw<ArgumentException>(() => FamilyVariant.Restore(
            FamilyVariantId.New(),
            ByColorAndSize,
            _catalog.TShirts.Id,
            [_catalog.Size.Id, _catalog.Size.Id],
            [_catalog.Gtin.Id, _catalog.Gtin.Id]));

        exception.Message.ShouldStartWith("Family variant 'by_color_and_size' cannot be restored.");
        exception.Message.ShouldContain($"Attribute {_catalog.Size.Id} is an axis more than once.");
        exception.Message.ShouldContain($"Attribute {_catalog.Gtin.Id} is a variant attribute more than once.");
        exception.Message.ShouldContain($"Axis {_catalog.Size.Id} is not a variant attribute.");
    }

    [Fact]
    public void RestoreRequiresAnAxis()
    {
        Should.Throw<ArgumentException>(() => FamilyVariant.Restore(
            FamilyVariantId.New(),
            ByColorAndSize,
            _catalog.TShirts.Id,
            [],
            [_catalog.Gtin.Id]));
    }

    private Result<FamilyVariant> Create(string[] axes, string[] variantAttributes) =>
        FamilyVariant.Create(
            ByColorAndSize,
            _catalog.TShirtSchema(),
            axes.Select(AttributeCode.Parse),
            variantAttributes.Select(AttributeCode.Parse));
}
