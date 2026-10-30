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
        Name.SettingsAs<TextSettings>().SetMaxLength(150);

        Description = AttributeDefinition.Create(
            AttributeCode.Parse("description"),
            AttributeType.TextArea,
            isLocalizable: true,
            isScopable: true).Value;

        Color = AttributeDefinition.Create(AttributeCode.Parse("color"), AttributeType.SingleSelect).Value;
        Red = Color.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("red")).Value;
        Blue = Color.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("blue")).Value;

        Weight = AttributeDefinition.CreateMeasurement(AttributeCode.Parse("weight"), MeasurementFamilies.Weight).Value;
        Gtin = AttributeDefinition.Create(AttributeCode.Parse("gtin"), AttributeType.Identifier).Value;
        Warranty = AttributeDefinition.Create(AttributeCode.Parse("warranty_years"), AttributeType.Number).Value;

        TShirts = Family.Create(FamilyCode.Parse("t_shirts")).Value;
        foreach (var attribute in new[] { Name, Description, Color, Weight, Gtin })
        {
            TShirts.AddAttribute(attribute);
        }

        TShirts.RequireAttribute(Name, Ecommerce.Id);
        TShirts.RequireAttribute(Description, Ecommerce.Id);
        TShirts.RequireAttribute(Color, Ecommerce.Id);
        TShirts.RequireAttribute(Name, Print.Id);

        Accessories = Family.Create(FamilyCode.Parse("accessories")).Value;
        Accessories.AddAttribute(Name);
        Accessories.AddAttribute(Warranty);
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

    public AttributeDefinition Weight { get; }

    public AttributeDefinition Gtin { get; }

    public AttributeDefinition Warranty { get; }

    public Family TShirts { get; }

    public Family Accessories { get; }

    public IReadOnlyList<AttributeDefinition> AllAttributes => [Name, Description, Color, Weight, Gtin, Warranty];

    public FamilySchema TShirtSchema() => FamilySchema.Create(TShirts, AllAttributes).Value;

    public FamilySchema AccessorySchema() => FamilySchema.Create(Accessories, AllAttributes).Value;
}
