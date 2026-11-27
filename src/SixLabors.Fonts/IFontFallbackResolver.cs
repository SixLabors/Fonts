// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Resolves a font family for code points that no configured font can shape.
/// The shaping pipeline consults the resolver when the assigned font and every
/// <see cref="TextOptions.FallbackFontFamilies"/> entry fail to supply a suitable glyph.
/// It consults the resolver at most once per distinct unresolved code point per shaping operation.
/// </summary>
public interface IFontFallbackResolver
{
    /// <summary>
    /// Tries to resolve a font family containing a glyph for the given code point.
    /// </summary>
    /// <param name="codePoint">The code point no configured font can shape.</param>
    /// <param name="requestedFamily">The family of the requested font, usable as a hint to bias matching toward stylistically compatible faces.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="culture">The culture used to select language specific faces, or <see langword="null"/>.</param>
    /// <param name="family">When this method returns <see langword="true"/>, the resolved font family.</param>
    /// <returns><see langword="true"/> if a family was resolved; otherwise, <see langword="false"/>.</returns>
    public bool TryResolve(CodePoint codePoint, FontFamily requestedFamily, FontStyle style, CultureInfo? culture, out FontFamily family);
}
