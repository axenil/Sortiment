namespace Sortiment.Core.Files;

/// <summary>
/// Stores file content, such as the files of assets. Implementations live in adapter packages, for example
/// <c>Sortiment.FileStorage.FileSystem</c>.
/// </summary>
/// <remarks>
/// Stored files are immutable: saving always creates a new file under a new key. Infrastructure failures, such as an
/// unreachable storage service, are reported as exceptions.
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
    /// <returns>The key of the saved file.</returns>
    Task<StorageKey> SaveAsync(Stream content, MediaType mediaType, CancellationToken cancellationToken);

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
