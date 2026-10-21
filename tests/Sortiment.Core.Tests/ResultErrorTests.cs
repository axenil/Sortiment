namespace Sortiment.Core.Tests;

public sealed class ResultErrorTests
{
    [Fact]
    public void NewErrorHasNoContext()
    {
        var error = new ResultError("test.code", "A message.");

        error.Code.ShouldBe("test.code");
        error.Message.ShouldBe("A message.");
        error.Context.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("", "A message.")]
    [InlineData(" ", "A message.")]
    [InlineData("test.code", "")]
    public void CodeAndMessageAreRequired(string code, string message)
    {
        Should.Throw<ArgumentException>(() => new ResultError(code, message));
    }

    [Fact]
    public void WithContextReturnsACopyAndLeavesTheOriginalUnchanged()
    {
        var original = new ResultError("test.code", "A message.");

        var withContext = original
            .WithContext(ErrorContextKeys.Attribute, "color")
            .WithContext(ErrorContextKeys.Value, "Red");

        original.Context.ShouldBeEmpty();
        withContext.Code.ShouldBe("test.code");
        withContext.Context.ShouldBe(
            new Dictionary<string, string>
            {
                [ErrorContextKeys.Attribute] = "color",
                [ErrorContextKeys.Value] = "Red",
            },
            ignoreOrder: true);
    }

    [Fact]
    public void WithContextReplacesAnExistingKey()
    {
        var error = new ResultError("test.code", "A message.")
            .WithContext(ErrorContextKeys.Value, "first")
            .WithContext(ErrorContextKeys.Value, "second");

        error.Context[ErrorContextKeys.Value].ShouldBe("second");
    }

    [Fact]
    public void ToStringShowsCodeAndMessage()
    {
        new ResultError("test.code", "A message.").ToString().ShouldBe("test.code: A message.");
    }
}
