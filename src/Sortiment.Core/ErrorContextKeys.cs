namespace Sortiment.Core;

/// <summary>
/// Well-known keys for <see cref="ResultError.Context"/>.
/// </summary>
public static class ErrorContextKeys
{
    /// <summary>
    /// The code of the attribute the error relates to.
    /// </summary>
    public const string Attribute = "attribute";

    /// <summary>
    /// The code of the attribute option the error relates to.
    /// </summary>
    public const string Option = "option";

    /// <summary>
    /// The invalid input value.
    /// </summary>
    public const string Value = "value";
}
