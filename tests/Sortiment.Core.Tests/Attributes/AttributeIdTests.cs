using Sortiment.Core.Attributes;

namespace Sortiment.Core.Tests.Attributes;

public sealed class AttributeIdTests
{
    [Fact]
    public void NewCreatesUniqueTimeOrderedIds()
    {
        var first = AttributeId.New();
        var second = AttributeId.New();

        first.ShouldNotBe(second);
        first.Value.Version.ShouldBe(7);
    }

    [Fact]
    public void AnExistingIdCanBeWrapped()
    {
        var guid = Guid.CreateVersion7();

        new AttributeId(guid).ShouldBe(new AttributeId(guid));
    }

    [Fact]
    public void AnEmptyIdIsRejected()
    {
        Should.Throw<ArgumentException>(() => new AttributeId(Guid.Empty));
        Should.Throw<ArgumentException>(() => new AttributeOptionId(Guid.Empty));
    }
}
