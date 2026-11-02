namespace Sortiment.Core.Tests.Fixtures;

/// <summary>
/// Assertions for steps that must succeed, so that a failing setup step fails the test instead of silently changing
/// what it tests.
/// </summary>
internal static class ResultAssertions
{
    public static void ShouldSucceed(this Result result) =>
        result.IsSuccess.ShouldBeTrue(Describe(result.Errors));

    public static T ShouldSucceed<T>(this Result<T> result)
    {
        result.IsSuccess.ShouldBeTrue(Describe(result.Errors));
        return result.Value;
    }

    private static string Describe(IReadOnlyList<ResultError> errors) =>
        $"Expected success, but got: {string.Join("; ", errors)}";
}
