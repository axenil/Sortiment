using Sortiment.Core.Assets;
using Sortiment.Core.Attributes;
using Sortiment.Core.Values;
using static Sortiment.Core.Tests.Assets.AssetTestData;

namespace Sortiment.Core.Tests.Attributes;

public sealed class AssetCollectionAttributeTests
{
    private static readonly AttributeCode Images = AttributeCode.Parse("images");
    private static readonly AssetId Front = AssetId.New();
    private static readonly AssetId Back = AssetId.New();
    private static readonly AssetId Side = AssetId.New();

    [Fact]
    public void CreateAssetCollectionLinksTheAttributeToTheAssetFamily()
    {
        var family = Packshots();

        var attribute = AttributeDefinition.CreateAssetCollection(Images, family, isLocalizable: true).Value;

        attribute.Type.ShouldBe(AttributeType.AssetCollection);
        attribute.AssetFamilyId.ShouldBe(family.Id);
        attribute.IsLocalizable.ShouldBeTrue();
        attribute.MaxCount.ShouldBeNull();
    }

    [Fact]
    public void CreateRefusesTheAssetCollectionTypeBecauseItNeedsAnAssetFamily()
    {
        Should.Throw<ArgumentException>(() => AttributeDefinition.Create(Images, AttributeType.AssetCollection));
    }

    [Fact]
    public void OtherAttributeTypesHaveNoAssetFamily()
    {
        AttributeDefinition.Create(Images, AttributeType.Text).Value.AssetFamilyId.ShouldBeNull();
    }

    [Fact]
    public void MaxCountCanBeSetAndRemoved()
    {
        var attribute = Create();

        attribute.SetMaxCount(1).IsSuccess.ShouldBeTrue();
        attribute.MaxCount.ShouldBe(1);

        attribute.SetMaxCount(null).IsSuccess.ShouldBeTrue();
        attribute.MaxCount.ShouldBeNull();
    }

    [Fact]
    public void MaxCountMustBePositive()
    {
        Create().SetMaxCount(0).Errors.ShouldHaveSingleItem().Code.ShouldBe("attribute.invalid_max_count");
    }

    [Fact]
    public void OtherAttributeTypesRejectAMaxCount()
    {
        AttributeDefinition.Create(Images, AttributeType.MultiSelect).Value.SetMaxCount(3).Errors
            .ShouldHaveSingleItem().Code.ShouldBe("attribute.constraint_not_supported");
    }

    [Fact]
    public void AnOrderedListOfAssetsIsValid()
    {
        Create().Validate(new AssetCollectionValue([Front, Back, Side]), ValueScope.Global).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AnEmptyCollectionIsRejected()
    {
        Create().Validate(new AssetCollectionValue([]), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.empty");
    }

    [Fact]
    public void EachDuplicatedAssetIsReportedOnce()
    {
        var errors = Create().Validate(new AssetCollectionValue([Front, Back, Front, Front, Back]), ValueScope.Global)
            .Errors;

        errors.Select(error => error.Code).ShouldBe(["value.duplicate_asset", "value.duplicate_asset"]);
        errors.Select(error => error.Context[ErrorContextKeys.Value]).ShouldBe([Front.ToString(), Back.ToString()]);
    }

    [Fact]
    public void MoreAssetsThanTheMaxCountAreRejected()
    {
        var attribute = Create();
        attribute.SetMaxCount(1);

        attribute.Validate(new AssetCollectionValue([Front]), ValueScope.Global).IsSuccess.ShouldBeTrue();
        attribute.Validate(new AssetCollectionValue([Front, Back]), ValueScope.Global).Errors.ShouldHaveSingleItem()
            .Code.ShouldBe("value.too_many_assets");
    }

    [Fact]
    public void AssetCollectionAttributesRejectOtherValueTypes()
    {
        Create().Validate(new TextValue("front.jpg"), ValueScope.Global).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("value.type_mismatch");
    }

    [Fact]
    public void ValuesWithTheSameAssetsInTheSameOrderAreEqual()
    {
        var first = new AssetCollectionValue([Front, Back]);
        var second = new AssetCollectionValue([Front, Back]);

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void OrderMattersForEquality()
    {
        new AssetCollectionValue([Front, Back]).ShouldNotBe(new AssetCollectionValue([Back, Front]));
    }

    [Fact]
    public void AValueCannotBeChangedThroughTheSourceCollection()
    {
        var source = new List<AssetId> { Front };
        var value = new AssetCollectionValue(source);

        source.Add(Back);

        value.Assets.ShouldBe([Front]);
    }

    private static AttributeDefinition Create() =>
        AttributeDefinition.CreateAssetCollection(Images, Packshots()).Value;
}
