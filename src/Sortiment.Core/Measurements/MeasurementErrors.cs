using System.Globalization;

namespace Sortiment.Core.Measurements;

internal static class MeasurementErrors
{
    public static ResultError UnitRequired(MeasurementFamilyCode family) =>
        new ResultError(
                "measurement_family.unit_required",
                $"Measurement family '{family}' must have at least one unit.")
            .WithContext(ErrorContextKeys.MeasurementFamily, family.Value);

    public static ResultError DuplicateUnit(MeasurementFamilyCode family, UnitCode unit) =>
        new ResultError(
                "measurement_family.duplicate_unit",
                $"Measurement family '{family}' has more than one unit '{unit}'.")
            .WithContext(ErrorContextKeys.MeasurementFamily, family.Value)
            .WithContext(ErrorContextKeys.Unit, unit.Value);

    public static ResultError InvalidConversionFactor(MeasurementFamilyCode family, MeasurementUnit unit) =>
        new ResultError(
                "measurement_family.invalid_conversion_factor",
                $"Unit '{unit.Code}' in measurement family '{family}' has the conversion factor {unit.ConversionFactor.ToString(CultureInfo.InvariantCulture)}. It must be greater than zero.")
            .WithContext(ErrorContextKeys.MeasurementFamily, family.Value)
            .WithContext(ErrorContextKeys.Unit, unit.Code.Value);

    public static ResultError UnknownUnit(MeasurementFamilyCode family, UnitCode unit) =>
        new ResultError(
                "measurement_family.unknown_unit",
                $"Measurement family '{family}' has no unit '{unit}'.")
            .WithContext(ErrorContextKeys.MeasurementFamily, family.Value)
            .WithContext(ErrorContextKeys.Unit, unit.Value);
}
