using System.Globalization;

namespace Sortiment.Core;

/// <summary>
/// Character rules shared by identifiers that people read and compare, such as SKUs and file names.
/// </summary>
internal static class CharacterRules
{
    /// <summary>
    /// Gets a value indicating whether a character is a control character or an invisible formatting character, such
    /// as a zero-width space or a right-to-left override. Such characters can make two different identifiers look
    /// identical, or make a file name display differently from what it is.
    /// </summary>
    public static bool IsInvisible(char character) =>
        char.IsControl(character) || char.GetUnicodeCategory(character) == UnicodeCategory.Format;
}
