using System.Globalization;
using System.Text;

namespace Sortiment.Core.Assets;

/// <summary>
/// The immutable, human-readable key of an asset, for example <c>ts001_front</c>. Asset codes are unique within their
/// asset family.
/// </summary>
/// <remarks>
/// A code is 1–100 characters long and contains only lowercase letters <c>a-z</c>, digits <c>0-9</c> and underscores.
/// <see cref="Create"/> never normalizes input. Use <see cref="FromFileName"/> to derive a code from a file name.
/// </remarks>
public sealed record AssetCode
{
    private const string FallbackCode = "asset";

    private AssetCode(string value) => Value = value;

    /// <summary>
    /// Gets the code as a string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates an asset code. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns>The code, or every rule the input breaks.</returns>
    public static Result<AssetCode> Create(string? value) =>
        CodeRules.Validate(value, "Asset code").Map(valid => new AssetCode(valid));

    /// <summary>
    /// Creates an asset code from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The code.</param>
    /// <returns>The code.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid code.</exception>
    public static AssetCode Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <summary>
    /// Derives a code from a file name, for example <c>TS001-Front.jpg</c> becomes <c>ts001_front</c>.
    /// </summary>
    /// <remarks>
    /// The extension is dropped, letters are lowercased, accents are removed (<c>ä</c> becomes <c>a</c>), and every run
    /// of other characters becomes one underscore. A name with nothing usable becomes <c>asset</c>. The result is not
    /// guaranteed to be unique within a family; callers add a suffix when the code is taken.
    /// </remarks>
    /// <param name="fileName">The file name.</param>
    /// <returns>A valid code.</returns>
    public static AssetCode FromFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var builder = new StringBuilder();
        foreach (var character in Path.GetFileNameWithoutExtension(fileName).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(character);
            if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                builder.Append(lower);
            }
            else if (builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }
        }

        var code = builder.ToString();
        if (code.Length > CodeRules.MaxLength)
        {
            code = code[..CodeRules.MaxLength];
        }

        code = code.TrimEnd('_');
        return new AssetCode(code.Length == 0 ? FallbackCode : code);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
