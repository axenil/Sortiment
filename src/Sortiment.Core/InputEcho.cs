using System.Globalization;
using System.Text;

namespace Sortiment.Core;

/// <summary>
/// Formats invalid input for use in error messages and context, which end up in API responses and logs.
/// </summary>
internal static class InputEcho
{
    /// <summary>
    /// The maximum length of formatted input, including the trailing ellipsis of shortened input. It matches the
    /// maximum length of codes, so valid-length codes are never shortened.
    /// </summary>
    public const int MaxLength = 100;

    private const char Ellipsis = '…';

    /// <summary>
    /// Escapes control characters and invisible formatting characters as <c>\uXXXX</c>, so input cannot break or
    /// disguise a log line, and shortens the result to <see cref="MaxLength"/> characters.
    /// </summary>
    /// <remarks>
    /// Shortening happens after escaping and only between whole characters, so it never cuts an escape sequence or a
    /// surrogate pair. Only the part that fits is read, so very long input is cheap to format.
    /// </remarks>
    public static string Format(string value)
    {
        var builder = new StringBuilder();
        var lengthBeforeEllipsis = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.IsControl(rune) || Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format)
            {
                builder.Append(rune.IsBmp ? $@"\u{rune.Value:X4}" : $@"\U{rune.Value:X8}");
            }
            else
            {
                builder.Append(rune.ToString());
            }

            if (builder.Length > MaxLength)
            {
                builder.Length = lengthBeforeEllipsis;
                return builder.Append(Ellipsis).ToString();
            }

            if (builder.Length < MaxLength)
            {
                lengthBeforeEllipsis = builder.Length;
            }
        }

        return builder.ToString();
    }
}
