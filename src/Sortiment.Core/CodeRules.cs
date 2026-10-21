namespace Sortiment.Core;

/// <summary>
/// The format rules shared by all definition codes (see ADR 0002).
/// </summary>
internal static class CodeRules
{
    public const int MaxLength = 100;

    /// <param name="value">The candidate code.</param>
    /// <param name="kind">What the code identifies, used in error messages, for example "Attribute code".</param>
    public static Result<string> Validate(string? value, string kind)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Result.Failure<string>(new ResultError("code.empty", $"{kind} cannot be empty."));
        }

        var errors = new List<ResultError>();

        if (value.Length > MaxLength)
        {
            errors.Add(new ResultError(
                    "code.too_long",
                    $"{kind} is {value.Length} characters long. The maximum is {MaxLength}.")
                .WithContext(ErrorContextKeys.Value, value));
        }

        if (!value.All(IsAllowedCharacter))
        {
            errors.Add(new ResultError(
                    "code.invalid_characters",
                    $"{kind} '{value}' can only contain lowercase letters a-z, digits 0-9 and underscores.")
                .WithContext(ErrorContextKeys.Value, value));
        }

        return errors.Count == 0 ? Result.Success(value) : Result.Failure<string>(errors);
    }

    public static T ValueOrThrow<T>(Result<T> result, string parameterName) =>
        result.IsSuccess
            ? result.Value
            : throw new ArgumentException(string.Join(" ", result.Errors.Select(error => error.Message)), parameterName);

    private static bool IsAllowedCharacter(char character) =>
        character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';
}
