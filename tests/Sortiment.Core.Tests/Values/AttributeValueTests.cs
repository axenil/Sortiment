using Sortiment.Core.Attributes;
using Sortiment.Core.Values;

namespace Sortiment.Core.Tests.Values;

public sealed class AttributeValueTests
{
    private static readonly AttributeOptionId Red = AttributeOptionId.New();
    private static readonly AttributeOptionId Blue = AttributeOptionId.New();

    [Fact]
    public void ValuesAreComparedByValue()
    {
        new TextValue("Shirt").ShouldBe(new TextValue("Shirt"));
        new NumberValue(1.5m).ShouldBe(new NumberValue(1.5m));
        new DateValue(new DateOnly(2026, 1, 31)).ShouldBe(new DateValue(new DateOnly(2026, 1, 31)));
        new SingleSelectValue(Red).ShouldBe(new SingleSelectValue(Red));
        new TextValue("Shirt").ShouldNotBe(new TextValue("shirt"));
    }

    [Fact]
    public void ValuesOfDifferentTypesAreNeverEqual()
    {
        AttributeValue text = new TextValue("1");
        AttributeValue number = new NumberValue(1m);

        text.ShouldNotBe(number);
    }

    [Fact]
    public void TextIsStoredExactlyAsGiven()
    {
        new TextValue("  Shirt\n").Text.ShouldBe("  Shirt\n");
    }

    [Fact]
    public void TextCannotBeNull()
    {
        Should.Throw<ArgumentNullException>(() => new TextValue(null!));
    }

    [Fact]
    public void MultiSelectKeepsTheGivenOrder()
    {
        new MultiSelectValue([Blue, Red]).Options.ShouldBe([Blue, Red]);
    }

    [Fact]
    public void MultiSelectValuesWithTheSameOptionsInAnyOrderAreEqual()
    {
        var first = new MultiSelectValue([Red, Blue]);
        var second = new MultiSelectValue([Blue, Red]);

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void MultiSelectValuesWithDifferentOptionsAreNotEqual()
    {
        new MultiSelectValue([Red]).ShouldNotBe(new MultiSelectValue([Red, Blue]));
    }

    [Fact]
    public void MultiSelectCannotBeChangedThroughTheSourceCollection()
    {
        var source = new List<AttributeOptionId> { Red };
        var value = new MultiSelectValue(source);

        source.Add(Blue);

        value.Options.ShouldBe([Red]);
    }
}
