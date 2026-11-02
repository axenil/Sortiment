namespace Sortiment.Core.Tests;

public sealed class InputEchoTests
{
    [Theory]
    [InlineData("TS-001")]
    [InlineData("Linneskjorta, blå")]
    [InlineData("👕 t-shirt")]
    public void PlainInputIsUnchanged(string value)
    {
        InputEcho.Format(value).ShouldBe(value);
    }

    [Fact]
    public void InputUpToTheMaximumLengthIsNotShortened()
    {
        var value = new string('a', InputEcho.MaxLength);

        InputEcho.Format(value).ShouldBe(value);
    }

    [Fact]
    public void LongInputIsShortenedWithAnEllipsis()
    {
        var formatted = InputEcho.Format(new string('a', 10_000));

        formatted.Length.ShouldBe(InputEcho.MaxLength);
        formatted.ShouldBe(new string('a', InputEcho.MaxLength - 1) + "…");
    }

    [Theory]
    [InlineData("TS\n001", @"TS\u000A001")]
    [InlineData("TS\r\n001", @"TS\u000D\u000A001")]
    [InlineData("\u001b[31mred", @"\u001B[31mred")]
    [InlineData("manual\u202Efdp.exe", @"manual\u202Efdp.exe")]
    [InlineData("TS-001\u200B", @"TS-001\u200B")]
    [InlineData("tag\U000E0041", @"tag\U000E0041")]
    public void ControlAndFormatCharactersAreEscaped(string value, string expected)
    {
        InputEcho.Format(value).ShouldBe(expected);
    }

    [Fact]
    public void ShorteningNeverCutsAnEscapeSequence()
    {
        var formatted = InputEcho.Format(new string('a', InputEcho.MaxLength - 3) + "\n" + "tail");

        formatted.ShouldBe(new string('a', InputEcho.MaxLength - 3) + "…");
    }

    [Fact]
    public void ShorteningNeverSplitsASurrogatePair()
    {
        var formatted = InputEcho.Format(new string('a', InputEcho.MaxLength - 2) + "👕👕");

        formatted.ShouldBe(new string('a', InputEcho.MaxLength - 2) + "…");
    }
}
