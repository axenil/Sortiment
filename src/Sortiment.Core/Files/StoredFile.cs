namespace Sortiment.Core.Files;

/// <summary>
/// What an <see cref="IFileStorage"/> knows about content it has stored: its key, and the size and checksum of the
/// bytes it actually wrote.
/// </summary>
/// <remarks>
/// New content is described by the <see cref="StoredFile"/> that <see cref="IFileStorage.SaveAsync"/> returns, so its
/// size and checksum always match what was stored (ADR 0014). Storage creates one from its stored state when it
/// restores a file.
/// </remarks>
public sealed record StoredFile
{
    /// <summary>
    /// Describes stored content.
    /// </summary>
    /// <param name="key">The key of the stored content.</param>
    /// <param name="size">The size of the content in bytes.</param>
    /// <param name="checksum">The SHA-256 checksum of the content.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
    public StoredFile(StorageKey key, long size, FileChecksum checksum)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        ArgumentNullException.ThrowIfNull(checksum);

        Key = key;
        Size = size;
        Checksum = checksum;
    }

    /// <summary>
    /// Gets the key of the stored content.
    /// </summary>
    public StorageKey Key { get; }

    /// <summary>
    /// Gets the size of the content in bytes.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Gets the SHA-256 checksum of the content.
    /// </summary>
    public FileChecksum Checksum { get; }
}
