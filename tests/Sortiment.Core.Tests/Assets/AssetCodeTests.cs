using Sortiment.Core.Assets;

namespace Sortiment.Core.Tests.Assets;

// The format rules are shared with AttributeCode and covered in AttributeCodeTests.
public sealed class AssetCodeTests
{
    [Theory]
    [InlineData("TS001-Front.jpg", "ts001_front")]
    [InlineData("ts001_front.jpg", "ts001_front")]
    [InlineData("Linne skjorta (blå) 2.PNG", "linne_skjorta_bla_2")]
    [InlineData("Fläsk & Äpple.webp", "flask_apple")]
    [InlineData("  --manual--  .pdf", "manual")]
    [InlineData("archive.tar.gz", "archive_tar")]
    [InlineData("no-extension", "no_extension")]
    public void FromFileNameDerivesAValidCode(string fileName, string expected)
    {
        AssetCode.FromFileName(fileName).Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData("---.png")]
    [InlineData("製品.jpg")]
    public void FromFileNameFallsBackWhenNothingIsUsable(string fileName)
    {
        AssetCode.FromFileName(fileName).Value.ShouldBe("asset");
    }

    [Fact]
    public void FromFileNameTruncatesLongNamesWithoutATrailingUnderscore()
    {
        var code = AssetCode.FromFileName(new string('a', 99) + "-b.jpg");

        code.Value.ShouldBe(new string('a', 99));
    }

    [Fact]
    public void CreateDoesNotNormalize()
    {
        AssetCode.Create("TS001_front").Errors.ShouldHaveSingleItem().Code.ShouldBe("code.invalid_characters");
    }
}
