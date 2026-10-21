using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Files;

public sealed class StorageKeyTests
{
    [Fact]
    public void AKeyIsStoredAsGiven()
    {
        new StorageKey("2026/10/0192f3a1-7c4e.jpg").Value.ShouldBe("2026/10/0192f3a1-7c4e.jpg");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("line\nbreak")]
    public void InvalidKeysAreRejected(string value)
    {
        Should.Throw<ArgumentException>(() => new StorageKey(value));
    }

    [Fact]
    public void KeysLongerThanTheMaximumAreRejected()
    {
        Should.Throw<ArgumentException>(() => new StorageKey(new string('a', StorageKey.MaxLength + 1)));
    }
}
