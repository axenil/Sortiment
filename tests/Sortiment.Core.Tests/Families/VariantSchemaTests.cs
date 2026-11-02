using Sortiment.Core.Families;
using Sortiment.Core.Tests.Fixtures;

namespace Sortiment.Core.Tests.Families;

public sealed class VariantSchemaTests
{
    private readonly TShirtCatalog _catalog = new();
    private readonly FamilyVariant _familyVariant;

    public VariantSchemaTests()
    {
        _familyVariant = FamilyVariant.Create(
                FamilyVariantCode.Parse("by_color_and_size"),
                _catalog.TShirtSchema(),
                [_catalog.Color.Code, _catalog.Size.Code],
                [_catalog.Gtin.Code])
            .Value;
    }

    [Fact]
    public void AVariantSchemaTellsVariantAttributesFromModelAttributes()
    {
        var schema = new VariantSchema(_catalog.TShirtSchema(), _familyVariant);

        schema.Axes.ShouldBe([_catalog.Color, _catalog.Size]);
        schema.IsVariantAttribute(_catalog.Size.Id).ShouldBeTrue();
        schema.IsVariantAttribute(_catalog.Gtin.Id).ShouldBeTrue();
        schema.IsModelAttribute(_catalog.Name.Id).ShouldBeTrue();
        schema.IsModelAttribute(_catalog.Gtin.Id).ShouldBeFalse();
    }

    [Fact]
    public void AttributesOutsideTheFamilyAreAtNeitherLevel()
    {
        var schema = new VariantSchema(_catalog.TShirtSchema(), _familyVariant);

        _catalog.TShirts.RemoveAttribute(_catalog.Gtin.Id).ShouldSucceed();

        schema.IsVariantAttribute(_catalog.Gtin.Id).ShouldBeFalse();
        schema.IsModelAttribute(_catalog.Gtin.Id).ShouldBeFalse();
        schema.IsModelAttribute(_catalog.Warranty.Id).ShouldBeFalse();
    }

    [Fact]
    public void TheFamilyVariantMustBelongToTheSchemasFamily()
    {
        Should.Throw<ArgumentException>(() => new VariantSchema(_catalog.AccessorySchema(), _familyVariant));
    }
}
