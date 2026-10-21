using Sortiment.Core.Assets;
using Sortiment.Core.Values;

namespace Sortiment.Core.Attributes;

/// <summary>
/// The settings of <see cref="AttributeType.AssetCollection"/> attributes: the asset family their assets come from and
/// how many assets a value can hold.
/// </summary>
/// <remarks>
/// Whether a value's assets exist and belong to <see cref="AssetFamilyId"/> is not checked by
/// <see cref="AttributeDefinition.Validate"/>, because that needs the assets themselves.
/// </remarks>
public sealed class AssetCollectionSettings : AttributeSettings
{
    private readonly AttributeCode _attribute;

    internal AssetCollectionSettings(AttributeCode attribute, AssetFamilyId assetFamilyId)
    {
        _attribute = attribute;
        AssetFamilyId = assetFamilyId;
    }

    /// <summary>
    /// Gets the asset family whose assets values hold. It cannot change after the attribute is created.
    /// </summary>
    public AssetFamilyId AssetFamilyId { get; }

    /// <summary>
    /// Gets the maximum number of assets in a value, or <see langword="null"/> for no limit.
    /// </summary>
    public int? MaxCount { get; private set; }

    /// <summary>
    /// Sets the maximum number of assets in a value.
    /// </summary>
    /// <param name="maxCount">The maximum number of assets, or <see langword="null"/> for no limit.</param>
    /// <returns>Success, or the error <c>attribute.invalid_max_count</c> if the count is less than 1.</returns>
    public Result SetMaxCount(int? maxCount)
    {
        if (maxCount < 1)
        {
            return Result.Failure(AttributeErrors.InvalidMaxCount(_attribute, maxCount.Value));
        }

        MaxCount = maxCount;
        return Result.Success();
    }

    internal override IEnumerable<ResultError> ValidateValue(AttributeDefinition attribute, AttributeValue value) =>
        value is AssetCollectionValue assets
            ? ValidateAssets(attribute, assets)
            : [ValueErrors.TypeMismatch(attribute, value)];

    private IEnumerable<ResultError> ValidateAssets(AttributeDefinition attribute, AssetCollectionValue value)
    {
        if (value.Assets.Count == 0)
        {
            yield return ValueErrors.Empty(attribute);
            yield break;
        }

        var duplicates = value.Assets
            .GroupBy(asset => asset)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
        foreach (var duplicate in duplicates)
        {
            yield return ValueErrors.DuplicateAsset(attribute, duplicate);
        }

        if (MaxCount is { } maxCount && value.Assets.Count > maxCount)
        {
            yield return ValueErrors.TooManyAssets(attribute, value.Assets.Count, maxCount);
        }
    }
}
