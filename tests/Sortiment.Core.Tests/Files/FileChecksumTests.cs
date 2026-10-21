using System.Security.Cryptography;
using System.Text;
using Sortiment.Core.Files;

namespace Sortiment.Core.Tests.Files;

public sealed class FileChecksumTests
{
    // SHA-256 of the UTF-8 bytes of "abc", from FIPS 180-2.
    private const string AbcChecksum = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public void FromHashWritesLowercaseHex()
    {
        FileChecksum.FromHash(SHA256.HashData(Encoding.UTF8.GetBytes("abc"))).Value.ShouldBe(AbcChecksum);
    }

    [Fact]
    public void CreateAcceptsUppercaseAndStoresLowercase()
    {
        FileChecksum.Create(AbcChecksum.ToUpperInvariant()).Value.ShouldBe(FileChecksum.Create(AbcChecksum).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ba7816bf")]
    [InlineData("za7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    public void CreateRejectsAnythingButSha256Hex(string? value)
    {
        FileChecksum.Create(value).Errors.ShouldHaveSingleItem().Code.ShouldBe("file_checksum.invalid_format");
    }

    [Fact]
    public void FromHashRejectsHashesOfTheWrongLength()
    {
        Should.Throw<ArgumentException>(() => FileChecksum.FromHash(new byte[16]));
    }
}
