namespace Sortiment.Core.Files;

/// <summary>
/// Stores file content, such as the files of assets. Implementations live in adapter packages, for example
/// <c>Sortiment.FileStorage.FileSystem</c>.
/// </summary>
/// <remarks>
/// <para>
/// Stored files are immutable: saving always creates a new file under a new key. Implementations generate the keys,
/// and only ever resolve keys that are valid <see cref="StorageKey"/>s, so a key cannot point outside the storage.
/// </para>
/// <para>
/// A missing file is the only expected failure, and is returned as a result when reading. Saving and deleting have no
/// expected failures. Infrastructure failures, such as a full disk or an unreachable storage service, are reported as
/// exceptions (ADR 0014).
/// </para>
/// </remarks>
public interface IFileStorage
{
    /// <summary>
    /// Saves file content under a new storage key.
    /// </summary>
    /// <param name="content">The content to save. It is read to the end but not closed.</param>
    /// <param name="mediaType">
    /// The media type of the content. Implementations that serve files directly, such as cloud storage behind a CDN,
    /// store it with the file.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The key of the saved file, with the size and SHA-256 checksum of the bytes that were actually written.
    /// Implementations measure both while writing; they never take them from the caller.
    /// </returns>
    Task<StoredFile> SaveAsync(Stream content, MediaType mediaType, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a stored file for reading. The caller disposes the returned stream.
    /// </summary>
    /// <param name="key">The key of the file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A readable stream, or the error <c>file_storage.not_found</c> if no file has the key.</returns>
    Task<Result<Stream>> OpenReadAsync(StorageKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a stored file. Deleting a file that does not exist succeeds, so cleanup can safely be retried.
    /// </summary>
    /// <param name="key">The key of the file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the file is deleted.</returns>
    Task DeleteAsync(StorageKey key, CancellationToken cancellationToken);
}
