using Sortiment.Core.Measurements;

namespace Sortiment.Core.Tests.Measurements;

public sealed class MeasurementIdTests
{
    [Fact]
    public void NewCreatesUniqueTimeOrderedIds()
    {
        MeasurementFamilyId.New().ShouldNotBe(MeasurementFamilyId.New());
        MeasurementFamilyId.New().Value.Version.ShouldBe(7);
        MeasurementUnitId.New().ShouldNotBe(MeasurementUnitId.New());
        MeasurementUnitId.New().Value.Version.ShouldBe(7);
    }

    [Fact]
    public void AnExistingIdCanBeWrapped()
    {
        var guid = Guid.CreateVersion7();

        new MeasurementFamilyId(guid).Value.ShouldBe(guid);
        new MeasurementUnitId(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void AnEmptyIdIsRejected()
    {
        Should.Throw<ArgumentException>(() => new MeasurementFamilyId(Guid.Empty));
        Should.Throw<ArgumentException>(() => new MeasurementUnitId(Guid.Empty));
    }
}
