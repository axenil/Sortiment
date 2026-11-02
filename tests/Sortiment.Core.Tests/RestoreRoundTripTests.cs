using Sortiment.Core.Assets;
using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Families;
using Sortiment.Core.Measurements;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Assets.AssetTestData;
using static Sortiment.Core.Tests.Fixtures.TShirtCatalog;

namespace Sortiment.Core.Tests;

/// <summary>
/// Restores each aggregate from nothing but its public properties, the way storage does, to prove that the public
/// state is enough to persist it (ADR 0011).
/// </summary>
public sealed class RestoreRoundTripTests
{
    private readonly TShirtCatalog _catalog = new();

    [Fact]
    public void ChannelsRoundTrip()
    {
        var channel = _catalog.Ecommerce;

        var restored = Channel.Restore(channel.Id, channel.Code, channel.Locales);

        restored.Id.ShouldBe(channel.Id);
        restored.Code.ShouldBe(channel.Code);
        restored.Locales.ShouldBe(channel.Locales);
    }

    [Fact]
    public void AttributesOfEveryTypeRoundTrip()
    {
        foreach (var attribute in AttributesOfEveryType())
        {
            var restored = RestoreAttribute(attribute);

            restored.Id.ShouldBe(attribute.Id);
            restored.Code.ShouldBe(attribute.Code);
            restored.Type.ShouldBe(attribute.Type);
            restored.IsLocalizable.ShouldBe(attribute.IsLocalizable);
            restored.IsScopable.ShouldBe(attribute.IsScopable);
            SettingsState(restored.Settings).ShouldBe(SettingsState(attribute.Settings), attribute.Code.Value);
        }
    }

    [Fact]
    public void FamiliesRoundTrip()
    {
        var family = _catalog.TShirts;

        var restored = Family.Restore(family.Id, family.Code, family.Attributes, family.Requirements);

        restored.Id.ShouldBe(family.Id);
        restored.Code.ShouldBe(family.Code);
        restored.Attributes.ShouldBe(family.Attributes);
        foreach (var channel in new[] { _catalog.Ecommerce, _catalog.Print })
        {
            restored.RequiredAttributes(channel.Id).ShouldBe(family.RequiredAttributes(channel.Id));
        }
    }

    [Fact]
    public void ProductsRoundTrip()
    {
        var schema = _catalog.TShirtSchema();
        var product = Product.Create(Sku.Parse("TS-001"), schema).Value;
        product.SetValue(schema, AttributeCode.Parse("name"), ValueScope.ForLocale(Swedish), new TextValue("Linneskjorta"));
        product.SetValue(
            schema,
            AttributeCode.Parse("description"),
            ValueScope.For(English, _catalog.Ecommerce.Id),
            new TextValue("A linen shirt."));
        product.SetValue(schema, AttributeCode.Parse("color"), ValueScope.Global, new SingleSelectValue(_catalog.Red.Id));
        product.SetValue(
            schema,
            AttributeCode.Parse("weight"),
            ValueScope.Global,
            new MeasurementValue(180m, UnitCode.Parse("g")));
        product.Disable();

        var restored = Product.Restore(product.Id, product.Sku, product.FamilyId, product.IsEnabled, product.Values);

        restored.Id.ShouldBe(product.Id);
        restored.Sku.Value.ShouldBe(product.Sku.Value);
        restored.FamilyId.ShouldBe(product.FamilyId);
        restored.IsEnabled.ShouldBe(product.IsEnabled);
        restored.Values.ShouldBe(product.Values, ignoreOrder: true);
    }

    [Fact]
    public void AssetFamiliesAndAssetsRoundTrip()
    {
        var family = Packshots(maxFileSize: 10_000_000);
        var asset = Asset.Create(family, AssetCode.Parse("ts001_front"), File()).Value;

        var restoredFamily = AssetFamily.Restore(family.Id, family.Code, family.AllowedMediaTypes, family.MaxFileSize);
        var restoredAsset = Asset.Restore(asset.Id, asset.FamilyId, asset.Code, asset.File);

        restoredFamily.Id.ShouldBe(family.Id);
        restoredFamily.Code.ShouldBe(family.Code);
        restoredFamily.AllowedMediaTypes.ShouldBe(family.AllowedMediaTypes);
        restoredFamily.MaxFileSize.ShouldBe(family.MaxFileSize);
        restoredAsset.Id.ShouldBe(asset.Id);
        restoredAsset.FamilyId.ShouldBe(asset.FamilyId);
        restoredAsset.Code.ShouldBe(asset.Code);
        restoredAsset.File.ShouldBe(asset.File);
    }

    private List<AttributeDefinition> AttributesOfEveryType()
    {
        var warranty = _catalog.Warranty;
        warranty.SettingsAs<NumberSettings>().SetRange(0m, 10m);
        warranty.SettingsAs<NumberSettings>().SetDecimalPlaces(0);

        var materials = AttributeDefinition.Create(AttributeCode.Parse("materials"), AttributeType.MultiSelect).Value;
        materials.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("linen"));
        materials.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("cotton"));

        var images = AttributeDefinition.CreateAssetCollection(
            AttributeCode.Parse("images"),
            Packshots(),
            isScopable: true).Value;
        images.SettingsAs<AssetCollectionSettings>().SetMaxCount(8);

        var attributes = new List<AttributeDefinition>(_catalog.AllAttributes)
        {
            AttributeDefinition.Create(AttributeCode.Parse("organic"), AttributeType.Boolean).Value,
            AttributeDefinition.Create(AttributeCode.Parse("launch_date"), AttributeType.Date).Value,
            materials,
            images,
        };

        attributes.Select(attribute => attribute.Type).Distinct().Count().ShouldBe(Enum.GetValues<AttributeType>().Length);
        return attributes;
    }

    private static AttributeDefinition RestoreAttribute(AttributeDefinition source)
    {
        var restored = source.Settings switch
        {
            MeasurementSettings measurement => AttributeDefinition.RestoreMeasurement(
                source.Id,
                source.Code,
                MeasurementFamilies.Find(measurement.Family.Code)!,
                source.IsLocalizable,
                source.IsScopable),
            AssetCollectionSettings assets => AttributeDefinition.RestoreAssetCollection(
                source.Id,
                source.Code,
                assets.AssetFamilyId,
                source.IsLocalizable,
                source.IsScopable),
            _ => AttributeDefinition.Restore(source.Id, source.Code, source.Type, source.IsLocalizable, source.IsScopable),
        };

        switch (source.Settings)
        {
            case TextSettings text:
                restored.SettingsAs<TextSettings>().SetMaxLength(text.MaxLength).IsSuccess.ShouldBeTrue();
                break;
            case NumberSettings number:
                restored.SettingsAs<NumberSettings>().SetRange(number.MinValue, number.MaxValue).IsSuccess.ShouldBeTrue();
                restored.SettingsAs<NumberSettings>().SetDecimalPlaces(number.DecimalPlaces).IsSuccess.ShouldBeTrue();
                break;
            case SelectSettings select:
                foreach (var option in select.Options)
                {
                    restored.SettingsAs<SelectSettings>().RestoreOption(option.Id, option.Code);
                }

                break;
            case AssetCollectionSettings assets:
                restored.SettingsAs<AssetCollectionSettings>().SetMaxCount(assets.MaxCount).IsSuccess.ShouldBeTrue();
                break;
        }

        return restored;
    }

    private static string SettingsState(AttributeSettings settings) =>
        settings switch
        {
            TextSettings text => $"text max={text.MaxLength}",
            NumberSettings number => $"number {number.MinValue}..{number.MaxValue} decimals={number.DecimalPlaces}",
            SelectSettings select => $"select {string.Join(",", select.Options.Select(option => $"{option.Id}:{option.Code}"))}",
            MeasurementSettings measurement => $"measurement {measurement.Family.Code}",
            AssetCollectionSettings assets => $"assets {assets.AssetFamilyId} max={assets.MaxCount}",
            BooleanSettings => "boolean",
            DateSettings => "date",
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.GetType().Name, "Unknown settings type."),
        };
}
