using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Files;

public sealed class MediaTypeTests
{
    [Theory]
    [InlineData("image/jpeg", "image", "jpeg")]
    [InlineData("application/pdf", "application", "pdf")]
    [InlineData("image/svg+xml", "image", "svg+xml")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application", "vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void CreateAcceptsValidMediaTypes(string value, string type, string subtype)
    {
        var mediaType = MediaType.Create(value).Value;

        mediaType.Type.ShouldBe(type);
        mediaType.Subtype.ShouldBe(subtype);
        mediaType.ToString().ShouldBe(value);
    }

    [Fact]
    public void MediaTypesAreStoredInLowercase()
    {
        MediaType.Parse("Image/JPEG").ShouldBe(MediaType.Parse("image/jpeg"));
        MediaType.Parse("Image/JPEG").ToString().ShouldBe("image/jpeg");
    }

    [Theory]
    [InlineData(null, "media_type.empty")]
    [InlineData("", "media_type.empty")]
    [InlineData("image", "media_type.invalid_format")]
    [InlineData("image/", "media_type.invalid_format")]
    [InlineData("/jpeg", "media_type.invalid_format")]
    [InlineData("image/*", "media_type.invalid_format")]
    [InlineData("text/plain; charset=utf-8", "media_type.invalid_format")]
    [InlineData("image/jpeg/extra", "media_type.invalid_format")]
    public void CreateRejectsInvalidMediaTypes(string? value, string expectedCode)
    {
        MediaType.Create(value).Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
    }
}
