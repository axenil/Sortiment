using System.Collections.ObjectModel;
using Sortiment.Core.Files;

namespace Sortiment.Core.Assets;

/// <summary>
/// A kind of asset, such as packshots or user manuals, that decides which files its assets accept.
/// </summary>
/// <remarks>
/// File rules are checked when an asset is created and when its file is replaced. Changing the rules does not
/// affect files that assets already have.
/// </remarks>
public sealed class AssetFamily
{
    private List<MediaTypePattern> _allowedMediaTypes;

    private AssetFamily(AssetFamilyId id, AssetFamilyCode code, List<MediaTypePattern> allowedMediaTypes, long? maxFileSize)
    {
        Id = id;
        Code = code;
        _allowedMediaTypes = allowedMediaTypes;
        MaxFileSize = maxFileSize;
    }

    /// <summary>
    /// Gets the identity of the family.
    /// </summary>
    public AssetFamilyId Id { get; }

    /// <summary>
    /// Gets the code that imports and the API use to refer to the family.
    /// </summary>
    public AssetFamilyCode Code { get; }

    /// <summary>
    /// Gets the media types that the family's files may have. Empty means that every media type is allowed.
    /// </summary>
    public ReadOnlyCollection<MediaTypePattern> AllowedMediaTypes => _allowedMediaTypes.AsReadOnly();

    /// <summary>
    /// Gets the maximum size of the family's files in bytes, or <see langword="null"/> for no limit.
    /// </summary>
    public long? MaxFileSize { get; private set; }

    /// <summary>
    /// Creates a new asset family.
    /// </summary>
    /// <param name="code">The family's code.</param>
    /// <param name="allowedMediaTypes">The media types its files may have. Empty or omitted allows every type.</param>
    /// <param name="maxFileSize">The maximum file size in bytes, or <see langword="null"/> for no limit.</param>
    /// <returns>
    /// The new family, or the errors <c>asset_family.duplicate_media_type</c> and
    /// <c>asset_family.invalid_max_file_size</c>.
    /// </returns>
    public static Result<AssetFamily> Create(
        AssetFamilyCode code,
        IEnumerable<MediaTypePattern>? allowedMediaTypes = null,
        long? maxFileSize = null)
    {
        ArgumentNullException.ThrowIfNull(code);

        var errors = new List<ResultError>();
        var patterns = DistinctPatterns(code, allowedMediaTypes ?? [], errors);
        if (maxFileSize < 1)
        {
            errors.Add(AssetErrors.InvalidMaxFileSize(code, maxFileSize.Value));
        }

        return errors.Count == 0
            ? Result.Success(new AssetFamily(AssetFamilyId.New(), code, patterns, maxFileSize))
            : Result.Failure<AssetFamily>(errors);
    }

    /// <summary>
    /// Replaces the media types that the family's files may have.
    /// </summary>
    /// <param name="allowedMediaTypes">The allowed media types. Empty allows every type.</param>
    /// <returns>Success, or the error <c>asset_family.duplicate_media_type</c> for each repeated pattern.</returns>
    public Result SetAllowedMediaTypes(IEnumerable<MediaTypePattern> allowedMediaTypes)
    {
        ArgumentNullException.ThrowIfNull(allowedMediaTypes);

        var errors = new List<ResultError>();
        var patterns = DistinctPatterns(Code, allowedMediaTypes, errors);
        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        _allowedMediaTypes = patterns;
        return Result.Success();
    }

    /// <summary>
    /// Sets the maximum size of the family's files.
    /// </summary>
    /// <param name="maxFileSize">The maximum file size in bytes, or <see langword="null"/> for no limit.</param>
    /// <returns>Success, or the error <c>asset_family.invalid_max_file_size</c> if the size is less than 1.</returns>
    public Result SetMaxFileSize(long? maxFileSize)
    {
        if (maxFileSize < 1)
        {
            return Result.Failure(AssetErrors.InvalidMaxFileSize(Code, maxFileSize.Value));
        }

        MaxFileSize = maxFileSize;
        return Result.Success();
    }

    /// <summary>
    /// Checks whether a file is accepted by the family.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns>
    /// Success, or the errors <c>asset.media_type_not_allowed</c> and <c>asset.file_too_large</c>.
    /// </returns>
    public Result ValidateFile(AssetFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var errors = new List<ResultError>();
        if (_allowedMediaTypes.Count > 0 && !_allowedMediaTypes.Any(pattern => pattern.Matches(file.MediaType)))
        {
            errors.Add(AssetErrors.MediaTypeNotAllowed(Code, file.MediaType, _allowedMediaTypes));
        }

        if (file.Size > MaxFileSize)
        {
            errors.Add(AssetErrors.FileTooLarge(Code, file.Size, MaxFileSize.Value));
        }

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    private static List<MediaTypePattern> DistinctPatterns(
        AssetFamilyCode code,
        IEnumerable<MediaTypePattern> patterns,
        List<ResultError> errors)
    {
        var distinct = new List<MediaTypePattern>();
        foreach (var pattern in patterns)
        {
            ArgumentNullException.ThrowIfNull(pattern, nameof(patterns));
            if (distinct.Contains(pattern))
            {
                errors.Add(AssetErrors.DuplicateMediaType(code, pattern));
            }
            else
            {
                distinct.Add(pattern);
            }
        }

        return distinct;
    }
}
