using System.Security.Cryptography;

namespace Sortiment.Core.Files;

/// <summary>
/// The SHA-256 hash of a file's content, written as 64 lowercase hexadecimal characters. Files with the same checksum
/// have the same content.
/// </summary>
public sealed record FileChecksum
{
    private const int HexLength = SHA256.HashSizeInBytes * 2;

    private FileChecksum(string value) => Value = value;

    /// <summary>
    /// Gets the checksum as 64 lowercase hexadecimal characters.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a checksum. Uppercase hexadecimal characters are accepted and stored in lowercase.
    /// </summary>
    /// <param name="value">The candidate checksum.</param>
    /// <returns>The checksum, or the error <c>file_checksum.invalid_format</c>.</returns>
    public static Result<FileChecksum> Create(string? value)
    {
        if (value is null || value.Length != HexLength || !value.All(char.IsAsciiHexDigit))
        {
            return Result.Failure<FileChecksum>(new ResultError(
                    "file_checksum.invalid_format",
                    $"A file checksum must be a SHA-256 hash of {HexLength} hexadecimal characters.")
                .WithContext(ErrorContextKeys.Value, value ?? string.Empty));
        }

        return Result.Success(new FileChecksum(value.ToLowerInvariant()));
    }

    /// <summary>
    /// Creates a checksum from a computed SHA-256 hash.
    /// </summary>
    /// <param name="hash">The 32-byte hash.</param>
    /// <returns>The checksum.</returns>
    /// <exception cref="ArgumentException"><paramref name="hash"/> is not 32 bytes long.</exception>
    public static FileChecksum FromHash(ReadOnlySpan<byte> hash)
    {
        if (hash.Length != SHA256.HashSizeInBytes)
        {
            throw new ArgumentException($"A SHA-256 hash is {SHA256.HashSizeInBytes} bytes long.", nameof(hash));
        }

        return new FileChecksum(Convert.ToHexStringLower(hash));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
