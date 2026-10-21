using Sortiment.Core.Attributes;

namespace Sortiment.Core.Tests.Attributes;

public sealed class AttributeDefinitionTests
{
    private static readonly AttributeCode Color = AttributeCode.Parse("color");
    private static readonly AttributeOptionCode Red = AttributeOptionCode.Parse("red");
    private static readonly AttributeOptionCode Blue = AttributeOptionCode.Parse("blue");

    [Fact]
    public void CreateReturnsAnAttributeWithTheGivenDefinition()
    {
        var attribute = AttributeDefinition
            .Create(AttributeCode.Parse("description"), AttributeType.TextArea, isLocalizable: true, isScopable: true)
            .Value;

        attribute.Code.ShouldBe(AttributeCode.Parse("description"));
        attribute.Type.ShouldBe(AttributeType.TextArea);
        attribute.IsLocalizable.ShouldBeTrue();
        attribute.IsScopable.ShouldBeTrue();
        attribute.Settings.ShouldBeOfType<TextSettings>();
    }

    [Fact]
    public void CreateGivesEachAttributeANewId()
    {
        var first = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;
        var second = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void CreateMakesAttributesGlobalByDefault()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        attribute.IsLocalizable.ShouldBeFalse();
        attribute.IsScopable.ShouldBeFalse();
    }

    [Fact]
    public void CreateRejectsALocalizableOrScopableIdentifierAndReportsBothProblems()
    {
        var result = AttributeDefinition.Create(
            AttributeCode.Parse("gtin"),
            AttributeType.Identifier,
            isLocalizable: true,
            isScopable: true);

        result.Errors.Select(error => error.Code).ShouldBe(
            ["attribute.identifier_cannot_be_localizable", "attribute.identifier_cannot_be_scopable"]);
        result.Errors.ShouldAllBe(error => error.Context[ErrorContextKeys.Attribute] == "gtin");
    }

    [Fact]
    public void CreateThrowsForAnUndefinedType()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => AttributeDefinition.Create(Color, (AttributeType)999));
    }

    [Theory]
    [InlineData(AttributeType.Text, typeof(TextSettings))]
    [InlineData(AttributeType.TextArea, typeof(TextSettings))]
    [InlineData(AttributeType.Identifier, typeof(TextSettings))]
    [InlineData(AttributeType.Boolean, typeof(BooleanSettings))]
    [InlineData(AttributeType.Number, typeof(NumberSettings))]
    [InlineData(AttributeType.Date, typeof(DateSettings))]
    [InlineData(AttributeType.SingleSelect, typeof(SelectSettings))]
    [InlineData(AttributeType.MultiSelect, typeof(SelectSettings))]
    public void EachTypeGetsTheMatchingSettings(AttributeType type, Type settingsType)
    {
        AttributeDefinition.Create(Color, type).Value.Settings.ShouldBeOfType(settingsType);
    }

    [Fact]
    public void SettingsAsReturnsTheSettingsOfTheMatchingType()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        attribute.SettingsAs<SelectSettings>().ShouldBeSameAs(attribute.Settings);
    }

    [Fact]
    public void SettingsAsThrowsForAnotherSettingsType()
    {
        var attribute = AttributeDefinition.Create(AttributeCode.Parse("name"), AttributeType.Text).Value;

        var exception = Should.Throw<InvalidOperationException>(() => attribute.SettingsAs<SelectSettings>());
        exception.Message.ShouldContain("TextSettings");
    }

    [Fact]
    public void AddOptionAppendsOptionsInOrder()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        var red = attribute.SettingsAs<SelectSettings>().AddOption(Red).Value;
        attribute.SettingsAs<SelectSettings>().AddOption(Blue);

        red.Code.ShouldBe(Red);
        attribute.SettingsAs<SelectSettings>().Options.Select(option => option.Code).ShouldBe([Red, Blue]);
    }

    [Fact]
    public void AddOptionRejectsADuplicateCode()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.MultiSelect).Value;
        attribute.SettingsAs<SelectSettings>().AddOption(Red);

        var result = attribute.SettingsAs<SelectSettings>().AddOption(AttributeOptionCode.Parse("red"));

        var error = result.Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("attribute.option_already_exists");
        error.Context[ErrorContextKeys.Option].ShouldBe("red");
        attribute.SettingsAs<SelectSettings>().Options.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveOptionRemovesTheOption()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;
        attribute.SettingsAs<SelectSettings>().AddOption(Red);
        attribute.SettingsAs<SelectSettings>().AddOption(Blue);

        var result = attribute.SettingsAs<SelectSettings>().RemoveOption(Red);

        result.IsSuccess.ShouldBeTrue();
        attribute.SettingsAs<SelectSettings>().Options.Select(option => option.Code).ShouldBe([Blue]);
    }

    [Fact]
    public void RemoveOptionReportsAMissingOption()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        var result = attribute.SettingsAs<SelectSettings>().RemoveOption(Red);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.option_not_found");
    }

    [Fact]
    public void FindOptionReturnsTheOptionOrNull()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;
        var red = attribute.SettingsAs<SelectSettings>().AddOption(Red).Value;

        attribute.SettingsAs<SelectSettings>().FindOption(Red).ShouldBeSameAs(red);
        attribute.SettingsAs<SelectSettings>().FindOption(Blue).ShouldBeNull();
    }

    [Fact]
    public void OptionsCannotBeModifiedFromOutside()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        ((ICollection<AttributeOption>)attribute.SettingsAs<SelectSettings>().Options).IsReadOnly.ShouldBeTrue();
    }
}
