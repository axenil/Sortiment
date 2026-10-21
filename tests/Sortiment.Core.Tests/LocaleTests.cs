namespace Sortiment.Core.Tests;

public sealed class LocaleTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("sv-SE")]
    [InlineData("fil-PH")]
    [InlineData("zh-Hant")]
    [InlineData("zh-Hant-TW")]
    [InlineData("es-419")]
    public void CreateAcceptsCanonicalTags(string code)
    {
        Locale.Create(code).Value.Code.ShouldBe(code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateRejectsAMissingTag(string? code)
    {
        Locale.Create(code).Errors.ShouldHaveSingleItem().Code.ShouldBe("locale.empty");
    }

    [Theory]
    [InlineData("sv-se")]
    [InlineData("SV-SE")]
    [InlineData("sv_SE")]
    [InlineData("zh-hant-TW")]
    [InlineData("english")]
    [InlineData("e")]
    [InlineData(" sv-SE")]
    [InlineData("sv-SE-")]
    public void CreateRejectsNonCanonicalOrMalformedTags(string code)
    {
        var error = Locale.Create(code).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("locale.invalid_format");
        error.Context[ErrorContextKeys.Value].ShouldBe(code);
    }

    [Fact]
    public void ParseThrowsForInvalidInput()
    {
        Should.Throw<ArgumentException>(() => Locale.Parse("sv_SE"));
    }

    [Fact]
    public void LocalesWithTheSameTagAreEqual()
    {
        Locale.Parse("sv-SE").ShouldBe(Locale.Parse("sv-SE"));
        Locale.Parse("sv-SE").ShouldNotBe(Locale.Parse("sv-FI"));
    }

    [Fact]
    public void ToStringReturnsTheTag()
    {
        Locale.Parse("sv-SE").ToString().ShouldBe("sv-SE");
    }
}
