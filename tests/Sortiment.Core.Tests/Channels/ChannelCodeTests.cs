using Sortiment.Core.Channels;

namespace Sortiment.Core.Tests.Channels;

// The format rules are shared with AttributeCode and covered in AttributeCodeTests.
public sealed class ChannelCodeTests
{
    [Fact]
    public void CreateAcceptsAValidCode()
    {
        ChannelCode.Create("google_shopping").Value.Value.ShouldBe("google_shopping");
    }

    [Fact]
    public void CreateRejectsAnInvalidCodeAndNamesTheKindOfCode()
    {
        var error = ChannelCode.Create("Web Shop").Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("code.invalid_characters");
        error.Message.ShouldStartWith("Channel code");
    }

    [Fact]
    public void ANewIdIsNeverEmpty()
    {
        ChannelId.New().Value.ShouldNotBe(Guid.Empty);
        Should.Throw<ArgumentException>(() => new ChannelId(Guid.Empty));
    }
}
