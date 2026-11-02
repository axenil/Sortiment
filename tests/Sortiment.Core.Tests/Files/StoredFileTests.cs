using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Files;

public sealed class StoredFileTests
{
    private static readonly StorageKey Key = new("2026/11/file.jpg");
    private static readonly FileChecksum Checksum = FileChecksum.FromHash(new byte[32]);

    [Fact]
    public void AStoredFileKeepsWhatTheStorageReported()
    {
        var file = new StoredFile(Key, 2048, Checksum);

        file.Key.ShouldBe(Key);
        file.Size.ShouldBe(2048);
        file.Checksum.ShouldBe(Checksum);
    }

    [Fact]
    public void ANegativeSizeIsAProgrammingError()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new StoredFile(Key, -1, Checksum));
    }
}
