using Sortiment.Core.Attributes;
using Sortiment.Core.Channels;
using Sortiment.Core.Families;
using Sortiment.Core.Tests.Fixtures;

namespace Sortiment.Core.Tests.Families;

public sealed class FamilySchemaTests
{
    private readonly AttributeDefinition _name = Attribute("name");
    private readonly AttributeDefinition _description = Attribute("description");
    private readonly AttributeDefinition _material = Attribute("material");
    private readonly Family _family = Family.Create(FamilyCode.Parse("t_shirts")).Value;

    public FamilySchemaTests()
    {
        _family.AddAttribute(_name).ShouldSucceed();
        _family.AddAttribute(_description).ShouldSucceed();
    }

    [Fact]
    public void CreateResolvesTheFamilyAttributesInOrderAndIgnoresOthers()
    {
        var schema = FamilySchema.Create(_family, [_material, _description, _name]).Value;

        schema.Family.ShouldBeSameAs(_family);
        schema.Attributes.ShouldBe([_name, _description]);
    }

    [Fact]
    public void CreateReportsEveryAttributeWithoutADefinition()
    {
        var result = FamilySchema.Create(_family, [_material]);

        result.Errors.Select(error => error.Code).ShouldBe(
            ["family_schema.attribute_missing", "family_schema.attribute_missing"]);
        result.Errors.Select(error => error.Context[ErrorContextKeys.AttributeId])
            .ShouldBe([_name.Id.ToString(), _description.Id.ToString()]);
    }

    [Fact]
    public void FindAttributeLooksUpFamilyAttributesByIdOrCode()
    {
        var schema = FamilySchema.Create(_family, [_name, _description, _material]).Value;

        schema.FindAttribute(_name.Id).ShouldBeSameAs(_name);
        schema.FindAttribute(AttributeCode.Parse("description")).ShouldBeSameAs(_description);
        schema.FindAttribute(_material.Id).ShouldBeNull();
        schema.FindAttribute(AttributeCode.Parse("material")).ShouldBeNull();
    }

    [Fact]
    public void AnAttributeRemovedFromTheFamilyDisappearsFromTheSchema()
    {
        var schema = FamilySchema.Create(_family, [_name, _description]).Value;

        _family.RemoveAttribute(_name.Id).ShouldSucceed();

        schema.Attributes.ShouldBe([_description]);
        schema.FindAttribute(_name.Id).ShouldBeNull();
        schema.FindAttribute(AttributeCode.Parse("name")).ShouldBeNull();
    }

    [Fact]
    public void AnAttributeAddedToTheFamilyNeedsANewSchema()
    {
        var schema = FamilySchema.Create(_family, [_name, _description]).Value;

        _family.AddAttribute(_material).ShouldSucceed();

        schema.Attributes.ShouldBe([_name, _description]);
        schema.FindAttribute(_material.Id).ShouldBeNull();
        FamilySchema.Create(_family, [_name, _description, _material]).Value.FindAttribute(_material.Id)
            .ShouldBeSameAs(_material);
    }

    [Fact]
    public void RequiredAttributesArePerChannelInFamilyOrder()
    {
        var ecommerce = ChannelId.New();
        var print = ChannelId.New();
        _family.RequireAttribute(_description, ecommerce).ShouldSucceed();
        _family.RequireAttribute(_name, ecommerce).ShouldSucceed();
        _family.RequireAttribute(_description, print).ShouldSucceed();

        var schema = FamilySchema.Create(_family, [_name, _description]).Value;

        schema.RequiredAttributes(ecommerce).ShouldBe([_name, _description]);
        schema.RequiredAttributes(print).ShouldBe([_description]);
        schema.RequiredAttributes(ChannelId.New()).ShouldBeEmpty();
    }

    [Fact]
    public void RequiredAttributesLeaveOutAttributesAddedAfterTheSchemaWasCreated()
    {
        var ecommerce = ChannelId.New();
        _family.RequireAttribute(_name, ecommerce).ShouldSucceed();
        var schema = FamilySchema.Create(_family, [_name, _description]).Value;

        _family.AddAttribute(_material).ShouldSucceed();
        _family.RequireAttribute(_material, ecommerce).ShouldSucceed();

        schema.RequiredAttributes(ecommerce).ShouldBe([_name]);
    }

    private static AttributeDefinition Attribute(string code) =>
        AttributeDefinition.Create(AttributeCode.Parse(code), AttributeType.Text).Value;
}
