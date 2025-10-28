// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Defines a particular format for text, including font face, size, and style attributes.
/// This class cannot be inherited.
/// </summary>
public sealed class Font
{
    private readonly Lazy<FontMetrics?> metrics;
    private readonly Lazy<string> fontName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class.
    /// </summary>
    /// <param name="family">The font family.</param>
    /// <param name="size">The size of the font in PT units.</param>
    public Font(FontFamily family, float size)
        : this(family, size, FontStyle.Regular)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class.
    /// </summary>
    /// <param name="family">The font family.</param>
    /// <param name="size">The size of the font in PT units.</param>
    /// <param name="style">The font style.</param>
    public Font(FontFamily family, float size, FontStyle style)
    {
        if (family == default)
        {
            throw new ArgumentException("Cannot use the default value type instance to create a font.", nameof(family));
        }

        this.Family = family;
        this.RequestedStyle = style;
        this.Size = size;
        this.metrics = new Lazy<FontMetrics?>(this.LoadInstanceInternal, true);
        this.fontName = new Lazy<string>(this.LoadFontName, true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class.
    /// </summary>
    /// <param name="prototype">The prototype.</param>
    /// <param name="style">The font style.</param>
    public Font(Font prototype, FontStyle style)
        : this(prototype?.Family ?? throw new ArgumentNullException(nameof(prototype)), prototype.Size, style)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class.
    /// </summary>
    /// <param name="prototype">The prototype.</param>
    /// <param name="size">The size of the font in PT units.</param>
    /// <param name="style">The font style.</param>
    public Font(Font prototype, float size, FontStyle style)
        : this(prototype?.Family ?? throw new ArgumentNullException(nameof(prototype)), size, style)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class.
    /// </summary>
    /// <param name="prototype">The prototype.</param>
    /// <param name="size">The size of the font in PT units.</param>
    public Font(Font prototype, float size)
        : this(prototype.Family, size, prototype.RequestedStyle)
    {
    }

    /// <summary>
    /// Gets the family.
    /// </summary>
    public FontFamily Family { get; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name => this.fontName.Value;

    /// <summary>
    /// Gets the size of the font in PT units.
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// Gets the font metrics.
    /// </summary>
    /// <exception cref="FontException">Font instance not found.</exception>
    public FontMetrics FontMetrics => this.metrics.Value ?? throw new FontException("Font instance not found.");

    /// <summary>
    /// Gets a value indicating whether this <see cref="Font"/> is bold.
    /// </summary>
    public bool IsBold => (this.FontMetrics.Description.Style & FontStyle.Bold) == FontStyle.Bold;

    /// <summary>
    /// Gets a value indicating whether this <see cref="Font"/> is italic.
    /// </summary>
    public bool IsItalic => (this.FontMetrics.Description.Style & FontStyle.Italic) == FontStyle.Italic;

    /// <summary>
    /// Gets the requested style.
    /// </summary>
    internal FontStyle RequestedStyle { get; }

    /// <summary>
    /// Gets the filesystem path to the font family source.
    /// </summary>
    /// <param name="path">
    /// When this method returns, contains the filesystem path to the font family source,
    /// if the path exists; otherwise, the default value for the type of the path parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the <see cref="Font" /> was created via a filesystem path; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetPath([NotNullWhen(true)] out string? path)
    {
        if (this == default)
        {
            FontsThrowHelper.ThrowDefaultInstance();
        }

        if (this.FontMetrics is FileFontMetrics fileMetrics)
        {
            path = fileMetrics.Path;
            return true;
        }

        path = null;
        return false;
    }

    /// <summary>
    /// Gets the glyph for the given codepoint.
    /// </summary>
    /// <param name="codePoint">The code point of the character.</param>
    /// <param name="glyph">
    /// When this method returns, contains the glyph for the given codepoint if the glyph
    /// is found; otherwise the default value. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains glyphs for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyphs(CodePoint codePoint, [NotNullWhen(true)] out Glyph? glyph)
        => this.TryGetGlyphs(codePoint, TextAttributes.None, ColorFontSupport.None, out glyph);

    /// <summary>
    /// Gets the glyph for the given codepoint.
    /// </summary>
    /// <param name="codePoint">The code point of the character.</param>
    /// <param name="support">Options for enabling color font support during layout and rendering.</param>
    /// <param name="glyph">
    /// When this method returns, contains the glyphs for the given codepoint and color support if the glyph
    /// is found; otherwise the default value. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains glyphs for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyphs(CodePoint codePoint, ColorFontSupport support, [NotNullWhen(true)] out Glyph? glyph)
        => this.TryGetGlyphs(codePoint, TextAttributes.None, support, out glyph);

    /// <summary>
    /// Gets the glyph for the given codepoint.
    /// </summary>
    /// <param name="codePoint">The code point of the character.</param>
    /// <param name="textAttributes">The text attributes to apply to the glyphs.</param>
    /// <param name="support">Options for enabling color font support during layout and rendering.</param>
    /// <param name="glyph">
    /// When this method returns, contains the glyph for the given codepoint, attributes, and color support if the glyph
    /// is found; otherwise the default value. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains glyphs for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyphs(
        CodePoint codePoint,
        TextAttributes textAttributes,
        ColorFontSupport support,
        [NotNullWhen(true)] out Glyph? glyph)
        => this.TryGetGlyph(codePoint, textAttributes, TextDecorations.None, LayoutMode.HorizontalTopBottom, support, out glyph);

    /// <summary>
    /// Gets the glyph for the given codepoint.
    /// </summary>
    /// <param name="codePoint">The code point of the character.</param>
    /// <param name="textAttributes">The text attributes to apply to the glyphs.</param>
    /// <param name="layoutMode">The layout mode to apply to the glyphs.</param>
    /// <param name="support">Options for enabling color font support during layout and rendering.</param>
    /// <param name="glyph">
    /// When this method returns, contains the glyph for the given codepoint, attributes, and color support if the glyph
    /// is found; otherwise the default value. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains glyphs for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyph(
        CodePoint codePoint,
        TextAttributes textAttributes,
        LayoutMode layoutMode,
        ColorFontSupport support,
        [NotNullWhen(true)] out Glyph? glyph)
        => this.TryGetGlyph(codePoint, textAttributes, TextDecorations.None, layoutMode, support, out glyph);

    /// <summary>
    /// Gets the glyph for the given codepoint.
    /// </summary>
    /// <param name="codePoint">The code point of the character.</param>
    /// <param name="textAttributes">The text attributes to apply to the glyphs.</param>
    /// <param name="textDecorations">The text decorations to apply to the glyphs.</param>
    /// <param name="layoutMode">The layout mode to apply to the glyphs.</param>
    /// <param name="support">Options for enabling color font support during layout and rendering.</param>
    /// <param name="glyph">
    /// When this method returns, contains the glyph for the given codepoint, attributes, and color support if the glyph
    /// is found; otherwise the default value. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains glyphs for the specified codepoint; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyph(
        CodePoint codePoint,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        LayoutMode layoutMode,
        ColorFontSupport support,
        [NotNullWhen(true)] out Glyph? glyph)
    {
        TextRun textRun = new() { Start = 0, End = 1, Font = this, TextAttributes = textAttributes, TextDecorations = textDecorations };
        if (this.FontMetrics.TryGetGlyphMetrics(codePoint, textAttributes, textDecorations, layoutMode, support, out GlyphMetrics? metrics))
        {
            glyph = new Glyph(metrics.CloneForRendering(textRun), this.Size);
            return true;
        }

        glyph = null;
        return false;
    }

    /// <summary>
    /// Gets the amount, in px units, the <paramref name="current"/> glyph should be offset if it is followed by
    /// the <paramref name="next"/> glyph.
    /// </summary>
    /// <param name="current">The current glyph.</param>
    /// <param name="next">The next glyph.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the kerning offset at.</param>
    /// <param name="vector">
    /// When this method returns, contains the offset, in font units, that should be applied to the
    /// <paramref name="current"/> glyph, if the offset is found; otherwise the default vector value.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains and offset for the glyph combination; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetKerningOffset(Glyph current, Glyph next, float dpi, out Vector2 vector)
    {
        if (this.FontMetrics.TryGetKerningOffset(current.GlyphMetrics.GlyphId, next.GlyphMetrics.GlyphId, out vector))
        {
            // Scale the result
            Vector2 scale = new Vector2(this.Size * dpi) / next.GlyphMetrics.ScaleFactor;
            vector *= scale;
            return true;
        }

        return false;
    }

    private string LoadFontName()
        => this.metrics.Value?.Description.FontName(this.Family.Culture) ?? string.Empty;

    private FontMetrics? LoadInstanceInternal()
    {
        if (this.Family.TryGetMetrics(this.RequestedStyle, out FontMetrics? metrics))
        {
            return metrics;
        }

        if ((this.RequestedStyle & FontStyle.Italic) == FontStyle.Italic)
        {
            // Can't find style requested and they want one that's at least partial italic.
            // Try the regular italic.
            if (this.Family.TryGetMetrics(FontStyle.Italic, out metrics))
            {
                return metrics;
            }
        }

        if ((this.RequestedStyle & FontStyle.Bold) == FontStyle.Bold)
        {
            // Can't find style requested and they want one that's at least partial bold.
            // Try the regular bold.
            if (this.Family.TryGetMetrics(FontStyle.Bold, out metrics))
            {
                return metrics;
            }
        }

        // Can't find style requested so let's just try returning the default.
        IEnumerable<FontStyle>? styles = this.Family.GetAvailableStyles();
        FontStyle defaultStyle = styles.Contains(FontStyle.Regular)
        ? FontStyle.Regular
        : styles.First();

        this.Family.TryGetMetrics(defaultStyle, out metrics);
        return metrics;
    }
}
