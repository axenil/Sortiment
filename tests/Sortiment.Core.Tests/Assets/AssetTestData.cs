using Sortiment.Core.Assets;
using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Assets;

internal static class AssetTestData
{
    public static AssetFile File(
        string mediaType = "image/jpeg",
        long size = 1024,
        string fileName = "ts001-front.jpg",
        string? storageKey = null) =>
        AssetFile.Create(
                new StorageKey(storageKey ?? Guid.NewGuid().ToString()),
                fileName,
                MediaType.Parse(mediaType),
                size,
                FileChecksum.FromHash(new byte[32]))
            .Value;

    public static AssetFamily Packshots(long? maxFileSize = null) =>
        AssetFamily.Create(AssetFamilyCode.Parse("packshots"), [MediaTypePattern.Parse("image/*")], maxFileSize).Value;
}
