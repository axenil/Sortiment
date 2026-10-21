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
        attribute.Options.ShouldBeEmpty();
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
    [InlineData(AttributeType.SingleSelect, true)]
    [InlineData(AttributeType.MultiSelect, true)]
    [InlineData(AttributeType.Text, false)]
    [InlineData(AttributeType.Number, false)]
    public void OnlySelectAttributesHaveOptions(AttributeType type, bool expected)
    {
        AttributeDefinition.Create(Color, type).Value.HasOptions.ShouldBe(expected);
    }

    [Fact]
    public void AddOptionAppendsOptionsInOrder()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        var red = attribute.AddOption(Red).Value;
        attribute.AddOption(Blue);

        red.Code.ShouldBe(Red);
        attribute.Options.Select(option => option.Code).ShouldBe([Red, Blue]);
    }

    [Fact]
    public void AddOptionRejectsADuplicateCode()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.MultiSelect).Value;
        attribute.AddOption(Red);

        var result = attribute.AddOption(AttributeOptionCode.Parse("red"));

        var error = result.Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("attribute.option_already_exists");
        error.Context[ErrorContextKeys.Option].ShouldBe("red");
        attribute.Options.Count.ShouldBe(1);
    }

    [Fact]
    public void AddOptionIsRejectedForAttributesWithoutOptions()
    {
        var attribute = AttributeDefinition.Create(AttributeCode.Parse("name"), AttributeType.Text).Value;

        var result = attribute.AddOption(Red);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.options_not_supported");
        attribute.Options.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveOptionRemovesTheOption()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;
        attribute.AddOption(Red);
        attribute.AddOption(Blue);

        var result = attribute.RemoveOption(Red);

        result.IsSuccess.ShouldBeTrue();
        attribute.Options.Select(option => option.Code).ShouldBe([Blue]);
    }

    [Fact]
    public void RemoveOptionReportsAMissingOption()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        var result = attribute.RemoveOption(Red);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.option_not_found");
    }

    [Fact]
    public void FindOptionReturnsTheOptionOrNull()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;
        var red = attribute.AddOption(Red).Value;

        attribute.FindOption(Red).ShouldBeSameAs(red);
        attribute.FindOption(Blue).ShouldBeNull();
    }

    [Fact]
    public void OptionsCannotBeModifiedFromOutside()
    {
        var attribute = AttributeDefinition.Create(Color, AttributeType.SingleSelect).Value;

        ((ICollection<AttributeOption>)attribute.Options).IsReadOnly.ShouldBeTrue();
    }
}
