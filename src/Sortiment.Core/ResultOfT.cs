namespace Sortiment.Core;

/// <summary>
/// The outcome of an operation that can fail in an expected way and produces a value on success.
/// A failed result carries every error that was found, not only the first.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>Create instances with <see cref="Result.Success{T}(T)"/> and <see cref="Result.Failure{T}(ResultError)"/>.</remarks>
public sealed class Result<T>
{
    private readonly T _value;

    internal Result(T value)
    {
        _value = value;
        Errors = [];
    }

    internal Result(IReadOnlyList<ResultError> errors)
    {
        _value = default!;
        Errors = errors;
    }

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
    /// Gets the value produced by a successful operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">The operation failed. Check <see cref="IsSuccess"/> first.</exception>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot read the value of a failed result. Errors: {string.Join("; ", Errors)}");

    /// <summary>
    /// Transforms the value of a successful result. A failed result keeps its errors.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="map">The transformation to apply to the value.</param>
    /// <returns>A result with the transformed value, or the original errors.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return IsSuccess ? Result.Success(map(_value)) : Result.Failure<TOut>(Errors);
    }
}
