using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Families;
using Sortiment.Core.Measurements;

namespace Sortiment.Core.Tests.Fixtures;

/// <summary>
/// A small, realistic catalog for tests: t-shirts sold in a Swedish and English web shop and a Swedish print catalog.
/// Every instance is independent, so tests can change it freely.
/// </summary>
internal sealed class TShirtCatalog
{
    public TShirtCatalog()
    {
        Ecommerce = Channel.Create(ChannelCode.Parse("ecommerce"), [Swedish, English]).Value;
        Print = Channel.Create(ChannelCode.Parse("print"), [Swedish]).Value;

        Name = AttributeDefinition.Create(AttributeCode.Parse("name"), AttributeType.Text, isLocalizable: true).Value;
        Name.SettingsAs<TextSettings>().SetMaxLength(150).ShouldSucceed();

        Description = AttributeDefinition.Create(
            AttributeCode.Parse("description"),
            AttributeType.TextArea,
            isLocalizable: true,
            isScopable: true).Value;

        Color = AttributeDefinition.Create(AttributeCode.Parse("color"), AttributeType.SingleSelect).Value;
        Red = Color.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("red")).Value;
        Blue = Color.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("blue")).Value;

        Size = AttributeDefinition.Create(AttributeCode.Parse("size"), AttributeType.SingleSelect).Value;
        Small = Size.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("s")).Value;
        Medium = Size.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("m")).Value;

        Weight = AttributeDefinition.CreateMeasurement(AttributeCode.Parse("weight"), MeasurementFamilies.Weight).Value;
        Gtin = AttributeDefinition.Create(AttributeCode.Parse("gtin"), AttributeType.Identifier).Value;
        Warranty = AttributeDefinition.Create(AttributeCode.Parse("warranty_years"), AttributeType.Number).Value;

        TShirts = Family.Create(FamilyCode.Parse("t_shirts")).Value;
        foreach (var attribute in new[] { Name, Description, Color, Size, Weight, Gtin })
        {
            TShirts.AddAttribute(attribute).ShouldSucceed();
        }

        TShirts.RequireAttribute(Name, Ecommerce.Id).ShouldSucceed();
        TShirts.RequireAttribute(Description, Ecommerce.Id).ShouldSucceed();
        TShirts.RequireAttribute(Color, Ecommerce.Id).ShouldSucceed();
        TShirts.RequireAttribute(Name, Print.Id).ShouldSucceed();

        ByColorAndSize = FamilyVariant.Create(
                FamilyVariantCode.Parse("by_color_and_size"),
                TShirtSchema(),
                [Color.Code, Size.Code],
                [Gtin.Code, Weight.Code])
            .Value;

        Accessories = Family.Create(FamilyCode.Parse("accessories")).Value;
        Accessories.AddAttribute(Name).ShouldSucceed();
        Accessories.AddAttribute(Warranty).ShouldSucceed();
    }

    public static Locale Swedish { get; } = Locale.Parse("sv-SE");

    public static Locale English { get; } = Locale.Parse("en-GB");

    public Channel Ecommerce { get; }

    public Channel Print { get; }

    public AttributeDefinition Name { get; }

    public AttributeDefinition Description { get; }

    public AttributeDefinition Color { get; }

    public AttributeOption Red { get; }

    public AttributeOption Blue { get; }

    public AttributeDefinition Size { get; }

    public AttributeOption Small { get; }

    public AttributeOption Medium { get; }

    public AttributeDefinition Weight { get; }

    public AttributeDefinition Gtin { get; }

    public AttributeDefinition Warranty { get; }

    public Family TShirts { get; }

    public Family Accessories { get; }

    /// <summary>
    /// Gets the t-shirt family variant: color and size are the axes, and the GTIN and weight are set per variant.
    /// </summary>
    public FamilyVariant ByColorAndSize { get; }

    public IReadOnlyList<AttributeDefinition> AllAttributes => [Name, Description, Color, Size, Weight, Gtin, Warranty];

    public FamilySchema TShirtSchema() => FamilySchema.Create(TShirts, AllAttributes).Value;

    public FamilySchema AccessorySchema() => FamilySchema.Create(Accessories, AllAttributes).Value;

    public VariantSchema ColorAndSizeSchema() => new(TShirtSchema(), ByColorAndSize);
}
