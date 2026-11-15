using Sortiment.Core.Categories;

namespace Sortiment.Core.Tests.Categories;

public sealed class CategoryCodeTests
{
    [Fact]
    public void AValidCodeIsKeptAsGiven()
    {
        CategoryCode.Create("linen_t_shirts").Value.Value.ShouldBe("linen_t_shirts");
        CategoryTreeCode.Create("web_shop").Value.Value.ShouldBe("web_shop");
    }

    [Theory]
    [InlineData(null, "code.empty")]
    [InlineData("Linen", "code.invalid_characters")]
    public void InvalidCodesAreRejected(string? value, string expectedCode)
    {
        CategoryCode.Create(value).Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
        CategoryTreeCode.Create(value).Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
    }

    [Fact]
    public void NewIdsAreUniqueAndEmptyIdsAreRejected()
    {
        CategoryId.New().ShouldNotBe(CategoryId.New());
        CategoryTreeId.New().ShouldNotBe(CategoryTreeId.New());
        Should.Throw<ArgumentException>(() => new CategoryId(Guid.Empty));
        Should.Throw<ArgumentException>(() => new CategoryTreeId(Guid.Empty));
    }
}
