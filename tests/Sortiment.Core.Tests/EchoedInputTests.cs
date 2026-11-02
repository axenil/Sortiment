using Sortiment.Core.Assets;
using Sortiment.Core.Attributes;
using Sortiment.Core.Files;
using Sortiment.Core.Products;

namespace Sortiment.Core.Tests;

public sealed class EchoedInputTests
{
    private const int MaxMessageLength = 400;

    private static readonly string LongInput = new('\u202E', 10_000);

    public static TheoryData<string, Func<string, IReadOnlyList<ResultError>>> Validators => new()
    {
        { "code", value => AttributeCode.Create(value).Errors },
        { "sku", value => Sku.Create(value).Errors },
        { "locale", value => Locale.Create(value).Errors },
        { "media type", value => MediaType.Create(value).Errors },
        { "media type pattern", value => MediaTypePattern.Create(value).Errors },
        { "file checksum", value => FileChecksum.Create(value).Errors },
        {
            "file name", value => AssetFile.Create(
                new StoredFile(new StorageKey("key"), 10, FileChecksum.FromHash(new byte[32])),
                value,
                MediaType.Parse("image/jpeg")).Errors
        },
    };

    [Theory]
    [MemberData(nameof(Validators))]
    public void LongInvalidInputIsShortenedAndEscaped(string kind, Func<string, IReadOnlyList<ResultError>> validate)
    {
        var errors = validate(LongInput);

        errors.ShouldNotBeEmpty(kind);
        foreach (var error in errors)
        {
            error.Message.Length.ShouldBeLessThan(MaxMessageLength, kind);
            error.Message.ShouldNotContain("\u202E", Case.Sensitive, kind);
            if (error.Context.TryGetValue(ErrorContextKeys.Value, out var value))
            {
                value.Length.ShouldBeLessThanOrEqualTo(InputEcho.MaxLength, kind);
                value.ShouldNotContain("\u202E", Case.Sensitive, kind);
            }
        }
    }

    [Fact]
    public void ALineBreakInAnInvalidCodeIsEscaped()
    {
        var error = AttributeCode.Create("net\nweight").Errors.ShouldHaveSingleItem();

        error.Message.ShouldContain(@"'net\u000Aweight'");
        error.Context[ErrorContextKeys.Value].ShouldBe(@"net\u000Aweight");
    }
}
