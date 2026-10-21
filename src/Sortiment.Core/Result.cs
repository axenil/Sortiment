using System.Collections.ObjectModel;

namespace Sortiment.Core;

/// <summary>
/// The outcome of an operation that can fail in an expected way and produces no value.
/// A failed result carries every error that was found, not only the first.
/// </summary>
/// <remarks>
/// This type also holds the factory methods for <see cref="Result{T}"/>, so the value type can be inferred:
/// <c>Result.Success(code)</c> instead of <c>Result&lt;AttributeCode&gt;.Success(code)</c>.
/// </remarks>
public sealed class Result
{
    private static readonly Result SuccessResult = new([]);

    private Result(IReadOnlyList<ResultError> errors) => Errors = errors;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the errors that caused the operation to fail. Empty when the operation succeeded.
    /// </summary>
    public IReadOnlyList<ResultError> Errors { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => SuccessResult;

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(ResultError error) => new(ToErrorList([error]));

    /// <summary>
    /// Creates a failed result with one or more errors.
    /// </summary>
    /// <param name="errors">The errors. Must contain at least one error.</param>
    /// <returns>A failed result.</returns>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result Failure(IEnumerable<ResultError> errors) => new(ToErrorList(errors));

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success<T>(T value) => new(value);

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <typeparam name="T">The type of the value the operation would have produced.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(ResultError error) => new(ToErrorList([error]));

    /// <summary>
    /// Creates a failed result with one or more errors.
    /// </summary>
    /// <typeparam name="T">The type of the value the operation would have produced.</typeparam>
    /// <param name="errors">The errors. Must contain at least one error.</param>
    /// <returns>A failed result.</returns>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result<T> Failure<T>(IEnumerable<ResultError> errors) => new(ToErrorList(errors));

    private static ReadOnlyCollection<ResultError> ToErrorList(IEnumerable<ResultError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var list = errors.ToArray();
        if (list.Length == 0)
        {
            throw new ArgumentException("A failed result needs at least one error.", nameof(errors));
        }

        if (list.Any(error => error is null))
        {
            throw new ArgumentException("Errors cannot contain null.", nameof(errors));
        }

        return list.AsReadOnly();
    }
}
