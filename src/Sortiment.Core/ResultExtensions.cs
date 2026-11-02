namespace Sortiment.Core;

internal static class ResultExtensions
{
    /// <summary>
    /// Returns the value of a successful result, or throws for a failed one. Used where a failure is a programming
    /// error or corrupt stored data rather than an expected outcome, such as in <c>Parse</c> and <c>Restore</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The result failed. The message lists every error.</exception>
    public static T ValueOrThrow<T>(this Result<T> result, string parameterName) =>
        result.IsSuccess
            ? result.Value
            : throw new ArgumentException(string.Join(" ", result.Errors.Select(error => error.Message)), parameterName);
}
