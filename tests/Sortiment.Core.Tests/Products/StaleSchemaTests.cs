using Sortiment.Core.Attributes;
using Sortiment.Core.Families;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests.Products;

/// <summary>
/// A schema created before an attribute was added to its family must not answer as if the attribute did not exist
/// (ADR 0017).
/// </summary>
public sealed class StaleSchemaTests
{
    private readonly TShirtCatalog _catalog = new();
    private readonly AttributeDefinition _careInstructions =
        AttributeDefinition.Create(AttributeCode.Parse("care_instructions"), AttributeType.TextArea, isLocalizable: true)
            .Value;

    [Fact]
    public void ProductOperationsRefuseASchemaCreatedBeforeARequiredAttributeWasAdded()
    {
        var staleSchema = _catalog.TShirtSchema();
        var product = Product.Create(Sku.Parse("TS-001"), staleSchema).Value;

        AddRequiredCareInstructions();

        Should.Throw<InvalidOperationException>(() => product.CalculateCompleteness(staleSchema, _catalog.Ecommerce));
        Should.Throw<InvalidOperationException>(() => product.Validate(staleSchema));
        Should.Throw<InvalidOperationException>(() => product.SetValue(
            staleSchema,
            _careInstructions.Code,
            ValueScope.ForLocale(Swedish),
            new TextValue("Tvättas i 40 grader.")));
    }

    [Fact]
    public void ANewSchemaReportsTheAddedRequirementAsMissing()
    {
        var product = Product.Create(Sku.Parse("TS-001"), _catalog.TShirtSchema()).Value;

        AddRequiredCareInstructions();
        var schema = FamilySchema.Create(_catalog.TShirts, [.. _catalog.AllAttributes, _careInstructions]).Value;

        product.CalculateCompleteness(schema, _catalog.Ecommerce).ForLocale(Swedish)!.MissingAttributes
            .ShouldContain(_careInstructions.Code);
    }

    [Fact]
    public void VariantOperationsRefuseAStaleFamilySchema()
    {
        var staleSchema = _catalog.ColorAndSizeSchema();
        var context = new VariantContext(
            staleSchema,
            ProductModel.Create(ProductModelCode.Parse("linen_shirt"), staleSchema).Value);
        var variant = Product.CreateVariant(Sku.Parse("TS-001-RED-M"), context).Value;

        AddRequiredCareInstructions();

        Should.Throw<InvalidOperationException>(() => staleSchema.Axes);
        Should.Throw<InvalidOperationException>(() => context.Model.Validate(staleSchema));
        Should.Throw<InvalidOperationException>(() => variant.CalculateCompleteness(context, _catalog.Ecommerce));
    }

    private void AddRequiredCareInstructions()
    {
        _catalog.TShirts.AddAttribute(_careInstructions).ShouldSucceed();
        _catalog.TShirts.RequireAttribute(_careInstructions, _catalog.Ecommerce.Id).ShouldSucceed();
    }
}
