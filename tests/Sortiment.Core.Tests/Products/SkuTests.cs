using Sortiment.Core.Products;

namespace Sortiment.Core.Tests.Products;

public sealed class SkuTests
{
    [Theory]
    [InlineData("TS-001-RED-M")]
    [InlineData("ts_001")]
    [InlineData("4711.20/B")]
    [InlineData("ABC 123")]
    public void CreateAcceptsCommonSkuFormats(string value)
    {
        Sku.Create(value).Value.Value.ShouldBe(value);
    }

    [Fact]
    public void SkusAreEqualRegardlessOfCasing()
    {
        var upper = Sku.Parse("TS-001");
        var lower = Sku.Parse("ts-001");

        upper.ShouldBe(lower);
        (upper == lower).ShouldBeTrue();
        upper.GetHashCode().ShouldBe(lower.GetHashCode());
        new HashSet<Sku> { upper }.Contains(lower).ShouldBeTrue();
    }

    [Fact]
    public void SkusKeepTheirCasing()
    {
        Sku.Parse("Ts-001").Value.ShouldBe("Ts-001");
        Sku.Parse("Ts-001").ToString().ShouldBe("Ts-001");
    }

    [Fact]
    public void DifferentSkusAreNotEqual()
    {
        Sku.Parse("TS-001").ShouldNotBe(Sku.Parse("TS-002"));
    }

    [Theory]
    [InlineData(null, "sku.empty")]
    [InlineData("", "sku.empty")]
    [InlineData("   ", "sku.empty")]
    [InlineData(" TS-001", "sku.surrounding_whitespace")]
    [InlineData("TS-001 ", "sku.surrounding_whitespace")]
    [InlineData("TS\n001", "sku.invalid_characters")]
    [InlineData("TS\t001", "sku.invalid_characters")]
    public void CreateRejectsInvalidSkus(string? value, string expectedCode)
    {
        Sku.Create(value).Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
    }

    [Fact]
    public void CreateRejectsSkusLongerThanTheMaximum()
    {
        Sku.Create(new string('A', Sku.MaxLength)).IsSuccess.ShouldBeTrue();
        Sku.Create(new string('A', Sku.MaxLength + 1)).Errors.ShouldHaveSingleItem().Code.ShouldBe("sku.too_long");
    }
}
