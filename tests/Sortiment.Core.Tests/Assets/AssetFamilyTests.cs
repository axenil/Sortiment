using Sortiment.Core.Assets;
using Sortiment.Core.Files;
using static Sortiment.Core.Tests.Assets.AssetTestData;

namespace Sortiment.Core.Tests.Assets;

public sealed class AssetFamilyTests
{
    private static readonly AssetFamilyCode Manuals = AssetFamilyCode.Parse("user_manuals");

    [Fact]
    public void CreateReturnsAFamilyWithItsFileRules()
    {
        var family = AssetFamily.Create(Manuals, [MediaTypePattern.Parse("application/pdf")], 5_000_000).Value;

        family.Code.ShouldBe(Manuals);
        family.AllowedMediaTypes.ShouldBe([MediaTypePattern.Parse("application/pdf")]);
        family.MaxFileSize.ShouldBe(5_000_000);
    }

    [Fact]
    public void AFamilyWithoutRulesAcceptsEveryFile()
    {
        var family = AssetFamily.Create(Manuals).Value;

        family.AllowedMediaTypes.ShouldBeEmpty();
        family.MaxFileSize.ShouldBeNull();
        family.ValidateFile(File("application/x-anything", size: long.MaxValue)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void CreateReportsDuplicatePatternsAndAnInvalidMaxFileSize()
    {
        var result = AssetFamily.Create(
            Manuals,
            [MediaTypePattern.Parse("application/pdf"), MediaTypePattern.Parse("Application/PDF")],
            0);

        result.Errors.Select(error => error.Code)
            .ShouldBe(["asset_family.duplicate_media_type", "asset_family.invalid_max_file_size"]);
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("application/pdf", false)]
    public void ValidateFileChecksTheMediaType(string mediaType, bool expected)
    {
        Packshots().ValidateFile(File(mediaType)).IsSuccess.ShouldBe(expected);
    }

    [Fact]
    public void ValidateFileReportsADisallowedTypeWithTheAllowedTypes()
    {
        var error = Packshots().ValidateFile(File("application/pdf")).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("asset.media_type_not_allowed");
        error.Message.ShouldContain("image/*");
        error.Context[ErrorContextKeys.AssetFamily].ShouldBe("packshots");
        error.Context[ErrorContextKeys.Value].ShouldBe("application/pdf");
    }

    [Fact]
    public void ValidateFileChecksTheSize()
    {
        var family = Packshots(maxFileSize: 1000);

        family.ValidateFile(File(size: 1000)).IsSuccess.ShouldBeTrue();
        family.ValidateFile(File(size: 1001)).Errors.ShouldHaveSingleItem().Code.ShouldBe("asset.file_too_large");
    }

    [Fact]
    public void ValidateFileReportsBothProblems()
    {
        Packshots(maxFileSize: 10).ValidateFile(File("application/pdf", size: 11)).Errors.Select(error => error.Code)
            .ShouldBe(["asset.media_type_not_allowed", "asset.file_too_large"]);
    }

    [Fact]
    public void FileRulesCanBeChanged()
    {
        var family = Packshots(maxFileSize: 10);

        family.SetAllowedMediaTypes([MediaTypePattern.Parse("application/pdf")]).IsSuccess.ShouldBeTrue();
        family.SetMaxFileSize(null).IsSuccess.ShouldBeTrue();

        family.ValidateFile(File("application/pdf", size: 1_000_000)).IsSuccess.ShouldBeTrue();
        family.ValidateFile(File("image/jpeg")).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void InvalidRuleChangesAreRejectedAndLeaveTheRulesUnchanged()
    {
        var family = Packshots(maxFileSize: 10);

        family.SetMaxFileSize(-5).Errors.ShouldHaveSingleItem().Code.ShouldBe("asset_family.invalid_max_file_size");
        family.SetAllowedMediaTypes([MediaTypePattern.Parse("image/png"), MediaTypePattern.Parse("image/png")])
            .Errors.ShouldHaveSingleItem().Code.ShouldBe("asset_family.duplicate_media_type");

        family.MaxFileSize.ShouldBe(10);
        family.AllowedMediaTypes.ShouldBe([MediaTypePattern.Parse("image/*")]);
    }
}
