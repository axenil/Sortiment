using Sortiment.Core.Families;

namespace Sortiment.Core.Tests.Families;

public sealed class FamilyVariantCodeTests
{
    [Fact]
    public void AValidCodeIsKeptAsGiven()
    {
        FamilyVariantCode.Create("by_color_and_size").Value.Value.ShouldBe("by_color_and_size");
    }

    [Theory]
    [InlineData(null, "code.empty")]
    [InlineData("By_Color", "code.invalid_characters")]
    public void InvalidCodesAreRejected(string? value, string expectedCode)
    {
        FamilyVariantCode.Create(value).Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
    }

    [Fact]
    public void NewIdsAreUniqueAndEmptyIdsAreRejected()
    {
        FamilyVariantId.New().ShouldNotBe(FamilyVariantId.New());
        Should.Throw<ArgumentException>(() => new FamilyVariantId(Guid.Empty));
    }
}
