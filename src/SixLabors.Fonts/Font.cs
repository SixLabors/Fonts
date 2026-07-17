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
    private readonly FontVariation[] variations;
    private readonly FontWeight? requestedWeight;
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
        this.variations = [];
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
    /// Initializes a new instance of the <see cref="Font"/> class with the specified variation axis settings.
    /// </summary>
    /// <param name="prototype">The prototype font providing family, size, and style.</param>
    /// <param name="variations">The variation axis settings to apply.</param>
    public Font(Font prototype, params FontVariation[] variations)
    {
        Guard.NotNull(prototype, nameof(prototype));
        Guard.NotNull(variations, nameof(variations));

        this.Family = prototype.Family;
        this.RequestedStyle = prototype.RequestedStyle;
        this.Size = prototype.Size;
        this.variations = variations;
        this.requestedWeight = prototype.requestedWeight;
        this.metrics = new Lazy<FontMetrics?>(this.LoadInstanceInternal, true);
        this.fontName = new Lazy<string>(this.LoadFontName, true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class with a numeric weight that the
    /// operating-system family resolves through its normal metrics lookup.
    /// </summary>
    /// <param name="prototype">The font supplying family, size, style, and variation settings.</param>
    /// <param name="weight">The requested numeric system-font weight.</param>
    private Font(Font prototype, FontWeight weight)
    {
        this.Family = prototype.Family;
        this.RequestedStyle = prototype.RequestedStyle;
        this.Size = prototype.Size;
        this.variations = prototype.variations;
        this.requestedWeight = weight;
        this.metrics = new Lazy<FontMetrics?>(this.LoadInstanceInternal, true);
        this.fontName = new Lazy<string>(this.LoadFontName, true);
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
    /// Gets the variation axis settings applied to this font.
    /// </summary>
    public ReadOnlySpan<FontVariation> Variations => this.variations;

    /// <summary>
    /// Gets the requested style.
    /// </summary>
    internal FontStyle RequestedStyle { get; }

    /// <summary>
    /// Creates a variable-font instance at the requested weight when the font exposes a
    /// registered weight axis. Static fonts are returned unchanged.
    /// </summary>
    /// <param name="weight">The requested weight.</param>
    /// <param name="applied"><see langword="true"/> when the weight axis was applied.</param>
    /// <returns>The variable-font instance, or this font for a static face.</returns>
    internal Font WithWeight(FontWeight weight, out bool applied)
    {
        applied = false;
        if (this.FontMetrics.TryGetVariationAxes(out ReadOnlyMemory<Tables.AdvancedTypographic.Variations.VariationAxis> axes))
        {
            // A variable face owns its complete weight range, so apply wght before asking the
            // system collection for another static face. This also preserves every other axis.
            bool hasWeightAxis = false;
            foreach (Tables.AdvancedTypographic.Variations.VariationAxis axis in axes.Span)
            {
                if (axis.Tag == KnownVariationAxes.Weight)
                {
                    hasWeightAxis = true;
                    break;
                }
            }

            if (hasWeightAxis)
            {
                applied = true;
                for (int i = 0; i < this.variations.Length; i++)
                {
                    if (this.variations[i].Tag != KnownVariationAxes.Weight)
                    {
                        continue;
                    }

                    if (this.variations[i].Value == (int)weight)
                    {
                        return this;
                    }

                    // Font instances are immutable and can be shared by concurrent layouts.
                    // Recreate every variation in storage owned by the new Font so replacing wght
                    // cannot alter the source Font, while preserving every other configured axis.
                    FontVariation[] variations = new FontVariation[this.variations.Length];
                    for (int variationIndex = 0; variationIndex < this.variations.Length; variationIndex++)
                    {
                        FontVariation variation = this.variations[variationIndex];
                        variations[variationIndex] = new FontVariation(variation.Tag, variation.Value);
                    }

                    variations[i] = new FontVariation(KnownVariationAxes.Weight, (int)weight);
                    return new Font(this, variations);
                }

                // Preserve axes such as wdth or opsz while appending wght. A new array is required
                // because the Font constructor retains the complete variation set for its life.
                FontVariation[] weightedVariations = new FontVariation[this.variations.Length + 1];
                this.variations.CopyTo(weightedVariations, 0);
                weightedVariations[^1] = new FontVariation(KnownVariationAxes.Weight, (int)weight);

                return new Font(this, weightedVariations);
            }
        }

        // System collections preserve the platform's family grouping. Use an exact face exposed
        // by that grouping before falling back to synthetic weight on the supplied static face.
        if (this.Family.TryGetMetrics(this.RequestedStyle, weight, out FontMetrics? systemMetrics)
            && !ReferenceEquals(systemMetrics, this.FontMetrics))
        {
            return new Font(this, weight);
        }

        return this;
    }

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
    /// Gets the glyph identifier for the given code point.
    /// </summary>
    /// <param name="codePoint">The code point of the character.</param>
    /// <param name="glyphId">
    /// When this method returns, contains the glyph identifier for the given code point if the glyph
    /// is found; otherwise <value>0</value>. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the face contains a glyph for the specified code point; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
        => this.FontMetrics.TryGetGlyphId(codePoint, out glyphId);

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
        FontMetrics fontMetrics = this.FontMetrics;
        if (fontMetrics.TryGetGlyphId(codePoint, out ushort glyphId))
        {
            TextRun textRun = new() { Start = 0, End = 1, Font = this, TextAttributes = textAttributes, TextDecorations = textDecorations };
            FontGlyphMetrics metrics = fontMetrics.GetGlyphMetrics(codePoint, glyphId, textAttributes, textDecorations, layoutMode, support);

            glyph = new(metrics.CloneForRendering(textRun), this.Size);
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
        FontMetrics? metrics = this.ResolveBaseMetrics();
        if (metrics is null)
        {
            return null;
        }

        // If variations are specified and the base metrics supports them, create a variation instance.
        if (this.variations.Length > 0)
        {
            StreamFontMetrics? streamMetrics = metrics switch
            {
                StreamFontMetrics s => s,
                FileFontMetrics f => f.StreamFontMetrics,
                MemoryFontMetrics m => m.StreamFontMetrics,
                _ => null
            };

            if (streamMetrics is not null)
            {
                return streamMetrics.CreateVariationInstance(this.variations);
            }
        }

        return metrics;
    }

    private FontMetrics? ResolveBaseMetrics()
    {
        // Numeric lookup belongs to FontFamily because it owns the mapping from a family request
        // to a concrete face. Only system-backed families implement this overload; custom
        // collections continue through the existing FontStyle lookup below.
        if (this.requestedWeight.HasValue
            && this.Family.TryGetMetrics(this.RequestedStyle, this.requestedWeight.Value, out FontMetrics? weightedMetrics))
        {
            return weightedMetrics;
        }

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
        ReadOnlySpan<FontStyle> styles = this.Family.GetAvailableStyles().Span;
        FontStyle defaultStyle = styles[0];
        foreach (FontStyle style in styles)
        {
            if (style == FontStyle.Regular)
            {
                defaultStyle = FontStyle.Regular;
                break;
            }
        }

        this.Family.TryGetMetrics(defaultStyle, out metrics);
        return metrics;
    }
}
