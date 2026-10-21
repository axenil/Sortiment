namespace Sortiment.Core.Assets;

/// <summary>
/// A file managed by Sortiment, such as a product image or a manual, that products can reference.
/// </summary>
/// <remarks>
/// An asset belongs to one <see cref="AssetFamily"/>, which decides which files it accepts. Its code is unique within
/// the family; that uniqueness is enforced by whoever stores assets, because a single asset cannot see the others.
/// </remarks>
public sealed class Asset
{
    private Asset(AssetId id, AssetFamilyId familyId, AssetCode code, AssetFile file)
    {
        Id = id;
        FamilyId = familyId;
        Code = code;
        File = file;
    }

    /// <summary>
    /// Gets the identity of the asset.
    /// </summary>
    public AssetId Id { get; }

    /// <summary>
    /// Gets the identity of the family the asset belongs to.
    /// </summary>
    public AssetFamilyId FamilyId { get; }

    /// <summary>
    /// Gets the code that imports and the API use to refer to the asset within its family.
    /// </summary>
    public AssetCode Code { get; }

    /// <summary>
    /// Gets the asset's current file.
    /// </summary>
    public AssetFile File { get; private set; }

    /// <summary>
    /// Creates a new asset in a family.
    /// </summary>
    /// <param name="family">The family the asset belongs to.</param>
    /// <param name="code">The asset's code, unique within the family.</param>
    /// <param name="file">The asset's file, already saved in file storage.</param>
    /// <returns>The new asset, or the errors from <see cref="AssetFamily.ValidateFile"/>.</returns>
    public static Result<Asset> Create(AssetFamily family, AssetCode code, AssetFile file)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(file);

        var validation = family.ValidateFile(file);
        return validation.IsSuccess
            ? Result.Success(new Asset(AssetId.New(), family.Id, code, file))
            : Result.Failure<Asset>(validation.Errors.Select(error => AddAssetContext(error, code)));
    }

    /// <summary>
    /// Replaces the asset's file, for example after a reshoot. Products that use the asset show the new file.
    /// </summary>
    /// <param name="family">The family the asset belongs to.</param>
    /// <param name="file">The new file, already saved in file storage under a new key.</param>
    /// <returns>
    /// The replaced file, whose stored content the caller should delete, or the errors from
    /// <see cref="AssetFamily.ValidateFile"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="family"/> is not the asset's family, or <paramref name="file"/> has the current file's storage
    /// key. Stored files never change, so a replacement always has a new key; reusing the key would make the caller
    /// delete the file the asset still uses.
    /// </exception>
    public Result<AssetFile> ReplaceFile(AssetFamily family, AssetFile file)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(file);
        if (family.Id != FamilyId)
        {
            throw new ArgumentException("The family must be the asset's own family.", nameof(family));
        }

        if (file.StorageKey == File.StorageKey)
        {
            throw new ArgumentException("A replacement file must be stored under a new storage key.", nameof(file));
        }

        var validation = family.ValidateFile(file);
        if (validation.IsFailure)
        {
            return Result.Failure<AssetFile>(validation.Errors.Select(error => AddAssetContext(error, Code)));
        }

        var replaced = File;
        File = file;
        return Result.Success(replaced);
    }

    private static ResultError AddAssetContext(ResultError error, AssetCode code) =>
        error.WithContext(ErrorContextKeys.Asset, code.Value);
}
