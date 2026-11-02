using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Files;

public sealed class StorageKeyTests
{
    [Theory]
    [InlineData("2026/10/0192f3a1-7c4e.jpg")]
    [InlineData("0192f3a1-7c4e-7d2a-9b1c-3f5e8a7b6c4d")]
    [InlineData("assets/packshots/ts_001.front.v2.png")]
    public void AKeyIsStoredAsGiven(string value)
    {
        new StorageKey(value).Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("line\nbreak")]
    public void InvalidKeysAreRejected(string value)
    {
        Should.Throw<ArgumentException>(() => new StorageKey(value));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("2026/../../etc/passwd")]
    [InlineData("2026/./file.jpg")]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/file.jpg")]
    [InlineData(@"2026\10\file.jpg")]
    [InlineData("2026//file.jpg")]
    [InlineData("2026/")]
    [InlineData("file name.jpg")]
    [InlineData("fil\u00e9.jpg")]
    public void KeysThatCouldEscapeTheStorageRootAreRejected(string value)
    {
        Should.Throw<ArgumentException>(() => new StorageKey(value));
    }

    [Fact]
    public void KeysLongerThanTheMaximumAreRejected()
    {
        Should.Throw<ArgumentException>(() => new StorageKey(new string('a', StorageKey.MaxLength + 1)));
    }
}
