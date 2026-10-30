namespace Sortiment.Core.Products;

/// <summary>
/// The stock keeping unit that identifies a product to people and other systems, for example <c>TS-001-RED-M</c>.
/// </summary>
/// <remarks>
/// SKUs are case-insensitive but case-preserving: <c>ts-001</c> and <c>TS-001</c> are equal, while <see cref="Value"/>
/// keeps the casing the SKU was created with. A SKU is 1–100 characters long, contains no control characters and does
/// not start or end with whitespace. Uniqueness across products is enforced by whoever stores them.
/// </remarks>
public sealed record Sku
{
    /// <summary>
    /// The maximum length of a SKU.
    /// </summary>
    public const int MaxLength = 100;

    private Sku(string value) => Value = value;

    /// <summary>
    /// Gets the SKU with the casing it was created with.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates input and creates a SKU. Use this for input that may be invalid.
    /// </summary>
    /// <param name="value">The candidate SKU.</param>
    /// <returns>
    /// The SKU, or the errors <c>sku.empty</c>, <c>sku.too_long</c>, <c>sku.invalid_characters</c> and
    /// <c>sku.surrounding_whitespace</c>.
    /// </returns>
    public static Result<Sku> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Sku>(new ResultError("sku.empty", "SKU cannot be empty."));
        }

        var errors = new List<ResultError>();
        if (value.Length > MaxLength)
        {
            errors.Add(Error("sku.too_long", $"SKU is {value.Length} characters long. The maximum is {MaxLength}.", value));
        }

        if (value.Any(char.IsControl))
        {
            errors.Add(Error("sku.invalid_characters", "SKU cannot contain control characters such as line breaks.", value));
        }

        if (value.Trim().Length != value.Length)
        {
            errors.Add(Error("sku.surrounding_whitespace", $"SKU '{value}' cannot start or end with whitespace.", value));
        }

        return errors.Count == 0 ? Result.Success(new Sku(value)) : Result.Failure<Sku>(errors);
    }

    /// <summary>
    /// Creates a SKU from a value that is known to be valid, such as a literal in code.
    /// </summary>
    /// <param name="value">The SKU.</param>
    /// <returns>The SKU.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid SKU.</exception>
    public static Sku Parse(string value) => CodeRules.ValueOrThrow(Create(value), nameof(value));

    /// <summary>
    /// Compares SKUs without regard to casing.
    /// </summary>
    /// <param name="other">The SKU to compare with.</param>
    /// <returns><see langword="true"/> if both SKUs are equal when casing is ignored.</returns>
    public bool Equals(Sku? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    private static ResultError Error(string code, string message, string value) =>
        new ResultError(code, message).WithContext(ErrorContextKeys.Value, value);
}
