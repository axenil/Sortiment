using Sortiment.Core.Attributes;

namespace Sortiment.Core.Tests.Attributes;

public sealed class AttributeCodeTests
{
    [Theory]
    [InlineData("color")]
    [InlineData("a")]
    [InlineData("net_weight_2")]
    [InlineData("2024_collection")]
    public void CreateAcceptsValidCodes(string value)
    {
        var result = AttributeCode.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(value);
    }

    [Fact]
    public void CreateAcceptsACodeOfMaximumLength()
    {
        AttributeCode.Create(new string('a', 100)).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateRejectsAMissingCode(string? value)
    {
        var result = AttributeCode.Create(value);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("code.empty");
    }

    [Fact]
    public void CreateRejectsACodeThatIsTooLong()
    {
        var result = AttributeCode.Create(new string('a', 101));

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("code.too_long");
    }

    [Theory]
    [InlineData("Color")]
    [InlineData("net weight")]
    [InlineData("net-weight")]
    [InlineData(" color")]
    [InlineData("färg")]
    public void CreateRejectsInvalidCharactersWithoutNormalizing(string value)
    {
        var result = AttributeCode.Create(value);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("code.invalid_characters");
        error.Context[ErrorContextKeys.Value].ShouldBe(value);
    }

    [Fact]
    public void CreateReportsEveryBrokenRule()
    {
        var result = AttributeCode.Create(new string('A', 101));

        result.Errors.Select(error => error.Code).ShouldBe(["code.too_long", "code.invalid_characters"]);
    }

    [Fact]
    public void ParseReturnsTheCodeForValidInput()
    {
        AttributeCode.Parse("color").Value.ShouldBe("color");
    }

    [Fact]
    public void ParseThrowsForInvalidInput()
    {
        var exception = Should.Throw<ArgumentException>(() => AttributeCode.Parse("Color"));

        exception.Message.ShouldContain("Attribute code 'Color'");
    }

    [Fact]
    public void CodesWithTheSameValueAreEqual()
    {
        AttributeCode.Parse("color").ShouldBe(AttributeCode.Parse("color"));
        AttributeCode.Parse("color").ShouldNotBe(AttributeCode.Parse("size"));
    }

    [Fact]
    public void ToStringReturnsTheCode()
    {
        AttributeCode.Parse("color").ToString().ShouldBe("color");
    }
}
