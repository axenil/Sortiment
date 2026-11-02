using Sortiment.Core.Measurements;

namespace Sortiment.Core.Tests.Fixtures;

internal static class MeasurementTestData
{
    public static MeasurementUnitId UnitId(this MeasurementFamily family, string code) =>
        family.FindUnit(UnitCode.Parse(code))?.Id
        ?? throw new ArgumentException($"Measurement family '{family.Code}' has no unit '{code}'.", nameof(code));
}
