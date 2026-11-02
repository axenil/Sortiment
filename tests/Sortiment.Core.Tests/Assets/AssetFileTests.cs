using Sortiment.Core.Assets;
using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Assets;

public sealed class AssetFileTests
{
    private static readonly StorageKey Key = new("key");
    private static readonly MediaType Jpeg = MediaType.Parse("image/jpeg");
    private static readonly FileChecksum Checksum = FileChecksum.FromHash(new byte[32]);

    [Fact]
    public void CreateDescribesTheFile()
    {
        var file = AssetFile.Create(Content(2048), "TS001-Front.jpg", Jpeg, new ImageDimensions(800, 600)).Value;

        file.StorageKey.ShouldBe(Key);
        file.FileName.ShouldBe("TS001-Front.jpg");
        file.MediaType.ShouldBe(Jpeg);
        file.Size.ShouldBe(2048);
        file.Checksum.ShouldBe(Checksum);
        file.Dimensions.ShouldBe(new ImageDimensions(800, 600));
    }

    [Fact]
    public void DimensionsAreOptional()
    {
        AssetFile.Create(Content(10), "manual.pdf", MediaType.Parse("application/pdf")).Value.Dimensions
            .ShouldBeNull();
    }

    [Theory]
    [InlineData(null, "asset_file.file_name_empty")]
    [InlineData(" ", "asset_file.file_name_empty")]
    [InlineData("images/front.jpg", "asset_file.invalid_file_name")]
    [InlineData("images\\front.jpg", "asset_file.invalid_file_name")]
    [InlineData("front\n.jpg", "asset_file.invalid_file_name")]
    [InlineData("manual‮fdp.exe", "asset_file.invalid_file_name")]
    [InlineData("front​.jpg", "asset_file.invalid_file_name")]
    public void CreateRejectsInvalidFileNames(string? fileName, string expectedCode)
    {
        AssetFile.Create(Content(10), fileName, Jpeg).Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
    }

    [Fact]
    public void CreateRejectsFileNamesLongerThanTheMaximum()
    {
        var fileName = new string('a', AssetFile.MaxFileNameLength - 3) + ".jpg";

        AssetFile.Create(Content(10), fileName, Jpeg).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("asset_file.invalid_file_name");
    }

    [Fact]
    public void CreateReportsAnEmptyFileTogetherWithAFileNameError()
    {
        AssetFile.Create(Content(0), "", Jpeg).Errors.Select(error => error.Code)
            .ShouldBe(["asset_file.file_name_empty", "asset_file.empty_file"]);
    }

    [Fact]
    public void ImageDimensionsMustBePositive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ImageDimensions(0, 600));
        Should.Throw<ArgumentOutOfRangeException>(() => new ImageDimensions(800, -1));
    }

    private static StoredFile Content(long size) => new(Key, size, Checksum);
}
