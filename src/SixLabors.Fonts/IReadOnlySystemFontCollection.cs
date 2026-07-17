// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a readonly collection of Operating System fonts.
/// </summary>
public interface IReadOnlySystemFontCollection : IReadOnlyFontCollection
{
    /// <summary>
    /// <para>
    /// Gets the collection of Operating System directories that were searched for font families.
    /// </para>
    /// </summary>
    public IEnumerable<string> SearchDirectories { get; }

    /// <summary>
    /// Tries to match a character to an installed system font.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested font family name, or <see langword="null"/>.</param>
    /// <param name="culture">The culture used for matching, or <see langword="null"/>.</param>
    /// <param name="match">
    /// When this method returns, contains the matched font if one was found; otherwise, the default value.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a matching system font was found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryMatchCharacter(CodePoint codePoint, FontStyle style, string? familyName, CultureInfo? culture, out FontMatch match);
}
