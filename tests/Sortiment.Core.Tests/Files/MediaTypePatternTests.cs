using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Files;

public sealed class MediaTypePatternTests
{
    [Theory]
    [InlineData("image/*", "image/jpeg", true)]
    [InlineData("image/*", "image/png", true)]
    [InlineData("image/*", "application/pdf", false)]
    [InlineData("application/pdf", "application/pdf", true)]
    [InlineData("application/pdf", "application/zip", false)]
    [InlineData("IMAGE/*", "image/webp", true)]
    public void MatchesComparesTypeAndSubtype(string pattern, string mediaType, bool expected)
    {
        MediaTypePattern.Parse(pattern).Matches(MediaType.Parse(mediaType)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("image/*", "image", null)]
    [InlineData("application/pdf", "application", "pdf")]
    public void CreateParsesExactTypesAndWildcards(string value, string type, string? subtype)
    {
        var pattern = MediaTypePattern.Create(value).Value;

        pattern.Type.ShouldBe(type);
        pattern.Subtype.ShouldBe(subtype);
        pattern.ToString().ShouldBe(value);
    }

    [Theory]
    [InlineData(null, "media_type_pattern.empty")]
    [InlineData("", "media_type_pattern.empty")]
    [InlineData("*/*", "media_type_pattern.invalid_format")]
    [InlineData("*", "media_type_pattern.invalid_format")]
    [InlineData("image/jp*", "media_type_pattern.invalid_format")]
    [InlineData("image", "media_type_pattern.invalid_format")]
    public void CreateRejectsInvalidPatterns(string? value, string expectedCode)
    {
        MediaTypePattern.Create(value).Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
    }
}
