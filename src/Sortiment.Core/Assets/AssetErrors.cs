using Sortiment.Core.Files;

namespace Sortiment.Core.Assets;

internal static class AssetErrors
{
    public static ResultError FileNameEmpty() =>
        new("asset_file.file_name_empty", "A file name cannot be empty.");

    public static ResultError InvalidFileName(string fileName) =>
        new ResultError(
                "asset_file.invalid_file_name",
                $"File name '{fileName}' must be at most {AssetFile.MaxFileNameLength} characters long and cannot contain a directory or control characters.")
            .WithContext(ErrorContextKeys.Value, fileName);

    public static ResultError EmptyFile() =>
        new("asset_file.empty_file", "A file cannot be empty.");

    public static ResultError DuplicateMediaType(AssetFamilyCode family, MediaTypePattern pattern) =>
        new ResultError(
                "asset_family.duplicate_media_type",
                $"Asset family '{family}' lists the media type '{pattern}' more than once.")
            .WithContext(ErrorContextKeys.AssetFamily, family.Value)
            .WithContext(ErrorContextKeys.Value, pattern.ToString());

    public static ResultError InvalidMaxFileSize(AssetFamilyCode family, long maxFileSize) =>
        new ResultError(
                "asset_family.invalid_max_file_size",
                $"The maximum file size of asset family '{family}' must be at least 1 byte, but was {maxFileSize}.")
            .WithContext(ErrorContextKeys.AssetFamily, family.Value);

    public static ResultError MediaTypeNotAllowed(
        AssetFamilyCode family,
        MediaType mediaType,
        IEnumerable<MediaTypePattern> allowed) =>
        new ResultError(
                "asset.media_type_not_allowed",
                $"Asset family '{family}' does not accept files of type '{mediaType}'. Allowed types: {string.Join(", ", allowed)}.")
            .WithContext(ErrorContextKeys.AssetFamily, family.Value)
            .WithContext(ErrorContextKeys.Value, mediaType.ToString());

    public static ResultError FileTooLarge(AssetFamilyCode family, long size, long maxFileSize) =>
        new ResultError(
                "asset.file_too_large",
                $"The file is {size} bytes. Asset family '{family}' accepts files of at most {maxFileSize} bytes.")
            .WithContext(ErrorContextKeys.AssetFamily, family.Value);
}
