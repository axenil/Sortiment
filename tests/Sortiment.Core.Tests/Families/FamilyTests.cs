using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Families;

namespace Sortiment.Core.Tests.Families;

public sealed class FamilyTests
{
    private static readonly FamilyCode TShirts = FamilyCode.Parse("t_shirts");
    private static readonly ChannelId Ecommerce = ChannelId.New();
    private static readonly ChannelId Print = ChannelId.New();

    private readonly AttributeDefinition _name = Attribute("name");
    private readonly AttributeDefinition _description = Attribute("description");
    private readonly AttributeDefinition _material = Attribute("material");

    [Fact]
    public void CreateReturnsAFamilyWithoutAttributes()
    {
        var family = Family.Create(TShirts).Value;

        family.Code.ShouldBe(TShirts);
        family.Attributes.ShouldBeEmpty();
        family.Requirements.ShouldBeEmpty();
    }

    [Fact]
    public void AddAttributeKeepsTheAttributesInOrder()
    {
        var family = Family.Create(TShirts).Value;

        family.AddAttribute(_name).IsSuccess.ShouldBeTrue();
        family.AddAttribute(_description).IsSuccess.ShouldBeTrue();

        family.Attributes.ShouldBe([_name.Id, _description.Id]);
        family.HasAttribute(_name.Id).ShouldBeTrue();
        family.HasAttribute(_material.Id).ShouldBeFalse();
    }

    [Fact]
    public void AddAttributeRejectsAnAttributeThatIsAlreadyInTheFamily()
    {
        var family = Family.Create(TShirts).Value;
        family.AddAttribute(_name);

        var error = family.AddAttribute(_name).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("family.attribute_already_in_family");
        error.Context[ErrorContextKeys.Family].ShouldBe("t_shirts");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("name");
        family.Attributes.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveAttributeRemovesTheAttributeAndItsRequirements()
    {
        var family = FamilyWith(_name, _description);
        family.RequireAttribute(_name, Ecommerce);
        family.RequireAttribute(_description, Ecommerce);
        family.RequireAttribute(_name, Print);

        family.RemoveAttribute(_name.Id).IsSuccess.ShouldBeTrue();

        family.Attributes.ShouldBe([_description.Id]);
        family.RequiredAttributes(Ecommerce).ShouldBe([_description.Id]);
        family.RequiredAttributes(Print).ShouldBeEmpty();
    }

    [Fact]
    public void RemoveAttributeReportsAnAttributeThatIsNotInTheFamily()
    {
        FamilyWith(_name).RemoveAttribute(_material.Id).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("family.attribute_not_in_family");
    }

    [Fact]
    public void RequirementsArePerChannel()
    {
        var family = FamilyWith(_name, _description, _material);

        family.RequireAttribute(_name, Ecommerce);
        family.RequireAttribute(_description, Ecommerce);
        family.RequireAttribute(_name, Print);

        family.RequiredAttributes(Ecommerce).ShouldBe([_name.Id, _description.Id]);
        family.RequiredAttributes(Print).ShouldBe([_name.Id]);
        family.RequiredAttributes(ChannelId.New()).ShouldBeEmpty();
    }

    [Fact]
    public void RequiredAttributesFollowTheFamilyOrder()
    {
        var family = FamilyWith(_name, _description, _material);

        family.RequireAttribute(_material, Ecommerce);
        family.RequireAttribute(_name, Ecommerce);

        family.RequiredAttributes(Ecommerce).ShouldBe([_name.Id, _material.Id]);
    }

    [Fact]
    public void RequiringAnAttributeTwiceHasNoFurtherEffect()
    {
        var family = FamilyWith(_name);

        family.RequireAttribute(_name, Ecommerce).IsSuccess.ShouldBeTrue();
        family.RequireAttribute(_name, Ecommerce).IsSuccess.ShouldBeTrue();

        family.RequiredAttributes(Ecommerce).ShouldBe([_name.Id]);
    }

    [Fact]
    public void OnlyAttributesInTheFamilyCanBeRequired()
    {
        var family = FamilyWith(_name);

        var error = family.RequireAttribute(_material, Ecommerce).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("family.attribute_not_in_family");
        error.Context[ErrorContextKeys.Attribute].ShouldBe("material");
        family.RequiredAttributes(Ecommerce).ShouldBeEmpty();
    }

    [Fact]
    public void UnrequireAttributeStopsRequiringIt()
    {
        var family = FamilyWith(_name, _description);
        family.RequireAttribute(_name, Ecommerce);
        family.RequireAttribute(_description, Ecommerce);

        family.UnrequireAttribute(_name.Id, Ecommerce);
        family.UnrequireAttribute(_material.Id, Ecommerce);
        family.UnrequireAttribute(_name.Id, Print);

        family.RequiredAttributes(Ecommerce).ShouldBe([_description.Id]);
    }

    [Fact]
    public void RequirementsExposeEveryChannelWithoutAllowingChanges()
    {
        var family = FamilyWith(_name, _description);
        family.RequireAttribute(_name, Ecommerce);
        family.RequireAttribute(_description, Print);

        var requirements = family.Requirements;
        ((ISet<AttributeId>)requirements[Ecommerce]).Add(_description.Id);

        requirements.Keys.ShouldBe([Ecommerce, Print], ignoreOrder: true);
        family.RequiredAttributes(Ecommerce).ShouldBe([_name.Id]);
    }

    private static AttributeDefinition Attribute(string code) =>
        AttributeDefinition.Create(AttributeCode.Parse(code), AttributeType.Text).Value;

    private static Family FamilyWith(params AttributeDefinition[] attributes)
    {
        var family = Family.Create(TShirts).Value;
        foreach (var attribute in attributes)
        {
            family.AddAttribute(attribute);
        }

        return family;
    }
}
