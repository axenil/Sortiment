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
                .WithContext(ErrorContextKeys.Value, InputEcho.Format(value)));
        }

        if (!value.All(IsAllowedCharacter))
        {
            var echo = InputEcho.Format(value);
            errors.Add(new ResultError(
                    "code.invalid_characters",
                    $"{kind} '{echo}' can only contain lowercase letters a-z, digits 0-9 and underscores.")
                .WithContext(ErrorContextKeys.Value, echo));
        }

        return errors.Count == 0 ? Result.Success(value) : Result.Failure<string>(errors);
    }

    private static bool IsAllowedCharacter(char character) =>
        character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';
}
