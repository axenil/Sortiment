using Sortiment.Core.Attributes;

namespace Sortiment.Core.Tests.Attributes;

// The format rules are shared with AttributeCode and covered in AttributeCodeTests.
public sealed class AttributeOptionCodeTests
{
    [Fact]
    public void CreateAcceptsAValidCode()
    {
        AttributeOptionCode.Create("dark_blue").Value.Value.ShouldBe("dark_blue");
    }

    [Fact]
    public void CreateRejectsAnInvalidCodeAndNamesTheKindOfCode()
    {
        var error = AttributeOptionCode.Create("Dark Blue").Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("code.invalid_characters");
        error.Message.ShouldStartWith("Attribute option code");
    }

    [Fact]
    public void ParseThrowsForInvalidInput()
    {
        Should.Throw<ArgumentException>(() => AttributeOptionCode.Parse(""));
    }
}
