using Sortiment.Core.Files;

namespace Sortiment.Core.Assets;

/// <summary>
/// A stored file and what is known about it. An <see cref="Asset"/> has one current file.
/// </summary>
public sealed record AssetFile
{
    /// <summary>
    /// The maximum length of a file name, which matches common file system limits.
    /// </summary>
    public const int MaxFileNameLength = 255;

    private AssetFile(
        StorageKey storageKey,
        string fileName,
        MediaType mediaType,
        long size,
        FileChecksum checksum,
        ImageDimensions? dimensions)
    {
        StorageKey = storageKey;
        FileName = fileName;
        MediaType = mediaType;
        Size = size;
        Checksum = checksum;
        Dimensions = dimensions;
    }

    /// <summary>
    /// Gets the key of the stored file in <see cref="IFileStorage"/>.
    /// </summary>
    public StorageKey StorageKey { get; }

    /// <summary>
    /// Gets the original name of the file, for example <c>TS001-Front.jpg</c>.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the media type of the file's content.
    /// </summary>
    public MediaType MediaType { get; }

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Gets the SHA-256 checksum of the file's content.
    /// </summary>
    public FileChecksum Checksum { get; }

    /// <summary>
    /// Gets the dimensions of an image, or <see langword="null"/> if the file is not an image or they are unknown.
    /// </summary>
    public ImageDimensions? Dimensions { get; }

    /// <summary>
    /// Describes a stored file.
    /// </summary>
    /// <param name="storageKey">The key of the stored file.</param>
    /// <param name="fileName">The original file name, without a directory.</param>
    /// <param name="mediaType">The media type of the content.</param>
    /// <param name="size">The size in bytes.</param>
    /// <param name="checksum">The SHA-256 checksum of the content.</param>
    /// <param name="dimensions">The dimensions of an image, if known.</param>
    /// <returns>
    /// The file, or the errors <c>asset_file.file_name_empty</c>, <c>asset_file.invalid_file_name</c> and
    /// <c>asset_file.empty_file</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
    public static Result<AssetFile> Create(
        StorageKey storageKey,
        string? fileName,
        MediaType mediaType,
        long size,
        FileChecksum checksum,
        ImageDimensions? dimensions = null)
    {
        ArgumentNullException.ThrowIfNull(storageKey);
        ArgumentNullException.ThrowIfNull(mediaType);
        ArgumentNullException.ThrowIfNull(checksum);
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        var errors = new List<ResultError>();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add(AssetErrors.FileNameEmpty());
        }
        else if (!IsValidFileName(fileName))
        {
            errors.Add(AssetErrors.InvalidFileName(fileName));
        }

        if (size == 0)
        {
            errors.Add(AssetErrors.EmptyFile());
        }

        return errors.Count == 0
            ? Result.Success(new AssetFile(storageKey, fileName!, mediaType, size, checksum, dimensions))
            : Result.Failure<AssetFile>(errors);
    }

    private static bool IsValidFileName(string fileName) =>
        fileName.Length <= MaxFileNameLength
        && !fileName.Any(character => character is '/' or '\\' || char.IsControl(character));
}
