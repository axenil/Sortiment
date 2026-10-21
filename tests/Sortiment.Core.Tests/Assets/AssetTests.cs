using Sortiment.Core.Assets;
using static Sortiment.Core.Tests.Assets.AssetTestData;

namespace Sortiment.Core.Tests.Assets;

public sealed class AssetTests
{
    private static readonly AssetCode Front = AssetCode.Parse("ts001_front");

    [Fact]
    public void CreateReturnsAnAssetInTheFamily()
    {
        var family = Packshots();
        var file = File();

        var asset = Asset.Create(family, Front, file).Value;

        asset.FamilyId.ShouldBe(family.Id);
        asset.Code.ShouldBe(Front);
        asset.File.ShouldBe(file);
    }

    [Fact]
    public void CreateRejectsAFileTheFamilyDoesNotAccept()
    {
        var error = Asset.Create(Packshots(), Front, File("application/pdf")).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("asset.media_type_not_allowed");
        error.Context[ErrorContextKeys.Asset].ShouldBe("ts001_front");
    }

    [Fact]
    public void ReplaceFileSwapsTheFileAndReturnsTheReplacedOne()
    {
        var family = Packshots();
        var original = File();
        var asset = Asset.Create(family, Front, original).Value;
        var replacement = File(fileName: "ts001-front-reshoot.jpg");

        var result = asset.ReplaceFile(family, replacement);

        result.Value.ShouldBe(original);
        asset.File.ShouldBe(replacement);
        asset.Code.ShouldBe(Front);
    }

    [Fact]
    public void ReplaceFileRejectsAFileTheFamilyDoesNotAcceptAndKeepsTheCurrentFile()
    {
        var family = Packshots(maxFileSize: 100);
        var original = File(size: 100);
        var asset = Asset.Create(family, Front, original).Value;

        var result = asset.ReplaceFile(family, File(size: 101));

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("asset.file_too_large");
        asset.File.ShouldBe(original);
    }

    [Fact]
    public void ReplaceFileRequiresTheAssetsOwnFamily()
    {
        var asset = Asset.Create(Packshots(), Front, File()).Value;

        Should.Throw<ArgumentException>(() => asset.ReplaceFile(Packshots(), File()));
    }

    [Fact]
    public void ReplaceFileRequiresANewStorageKey()
    {
        var family = Packshots();
        var asset = Asset.Create(family, Front, File(storageKey: "same")).Value;

        Should.Throw<ArgumentException>(() => asset.ReplaceFile(family, File(fileName: "other.jpg", storageKey: "same")));
    }
}
