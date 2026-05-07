// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents synthetic glyph metrics for an atomic inline placeholder.
/// </summary>
internal sealed class PlaceholderGlyphMetrics : GlyphMetrics
{
    private readonly TextPlaceholder placeholder;
    private readonly float pointSize;
    private readonly float dpi;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderGlyphMetrics"/> class.
    /// </summary>
    /// <param name="font">The font metrics used for shared line metrics and decoration settings.</param>
    /// <param name="placeholder">The placeholder dimensions and alignment settings.</param>
    /// <param name="pointSize">The point size used for layout.</param>
    /// <param name="dpi">The resolution used to convert placeholder pixels into layout units.</param>
    /// <param name="textRun">The text run this placeholder belongs to.</param>
    internal PlaceholderGlyphMetrics(
        StreamFontMetrics font,
        TextPlaceholder placeholder,
        float pointSize,
        float dpi,
        TextRun textRun)
        : base(
            font,
            0,
            CodePoint.ObjectReplacementChar,
            GetBounds(placeholder, pointSize, dpi, font),
            ToGlyphUnits(placeholder.Width, pointSize, dpi, font.ScaleFactor),
            ToGlyphUnits(placeholder.Height, pointSize, dpi, font.ScaleFactor),
            0,
            0,
            font.UnitsPerEm,
            Vector2.Zero,
            new Vector2(font.ScaleFactor),
            textRun,
            GlyphType.Placeholder)
    {
        this.placeholder = placeholder;
        this.pointSize = pointSize;
        this.dpi = dpi;
    }

    /// <inheritdoc/>
    internal override GlyphMetrics CloneForRendering(TextRun textRun)
        => new PlaceholderGlyphMetrics(
            this.FontMetrics,
            this.placeholder,
            this.pointSize,
            this.dpi,
            textRun);

    /// <inheritdoc/>
    internal override void RenderTo(
        IGlyphRenderer renderer,
        int graphemeIndex,
        Vector2 glyphOrigin,
        Vector2 decorationOrigin,
        GlyphLayoutMode mode,
        TextOptions options)
    {
        // Placeholders reserve layout space only; the caller owns the object rendering.
    }

    /// <summary>
    /// Converts the placeholder box into glyph bounds in the same synthetic font-unit space as its advances.
    /// </summary>
    /// <param name="placeholder">The placeholder dimensions and baseline offset.</param>
    /// <param name="pointSize">The point size used for layout.</param>
    /// <param name="dpi">The resolution used to convert placeholder pixels into layout units.</param>
    /// <param name="font">The font metrics used by glyph layout.</param>
    /// <returns>The placeholder bounds expressed in synthetic font units.</returns>
    private static Bounds GetBounds(TextPlaceholder placeholder, float pointSize, float dpi, StreamFontMetrics font)
    {
        float scaleFactor = font.ScaleFactor;
        float width = ToGlyphUnitsFloat(placeholder.Width, pointSize, dpi, scaleFactor);
        float height = ToGlyphUnitsFloat(placeholder.Height, pointSize, dpi, scaleFactor);
        float baselineOffset = ToGlyphUnitsFloat(placeholder.BaselineOffset, pointSize, dpi, scaleFactor);
        float lineHeight = font.UnitsPerEm;
        float metricsDelta = (font.HorizontalMetrics.LineHeight - lineHeight) * .5F;
        float ascender = font.HorizontalMetrics.Ascender - metricsDelta;
        float descender = Math.Abs(font.HorizontalMetrics.Descender) - metricsDelta;
        float coreHeight = ascender + descender + (2 * metricsDelta);
        float extra = lineHeight - coreHeight;

        // Top/middle/bottom align against the surrounding run font's normal
        // line box, expressed relative to the text baseline in Y-up font units.
        float lineTop = ascender + metricsDelta + (extra * .5F);
        float lineBottom = lineTop - lineHeight;
        float top = baselineOffset;
        float bottom = baselineOffset - height;

        switch (placeholder.Alignment)
        {
            case TextPlaceholderAlignment.AboveBaseline:
                top = height;
                bottom = 0;
                break;

            case TextPlaceholderAlignment.BelowBaseline:
                top = 0;
                bottom = -height;
                break;

            case TextPlaceholderAlignment.Top:
                top = lineTop;
                bottom = top - height;
                break;

            case TextPlaceholderAlignment.Bottom:
                // Top, middle, and bottom align against the full line-height
                // box, not just the ascender/descender band.
                bottom = lineBottom;
                top = bottom + height;
                break;

            case TextPlaceholderAlignment.Middle:
                float center = (lineTop + lineBottom) * .5F;
                top = center + (height * .5F);
                bottom = center - (height * .5F);
                break;

            default:
                top = baselineOffset;
                bottom = baselineOffset - height;
                break;
        }

        // Placeholder bounds are authored in device pixels and converted into
        // synthetic font units so the normal glyph scaling path maps them back
        // to device-space size while preserving the requested baseline alignment.
        return new Bounds(0, top, width, bottom);
    }

    /// <summary>
    /// Converts a placeholder pixel measurement into glyph units for the current layout scale.
    /// </summary>
    /// <param name="pixels">The placeholder measurement in pixels.</param>
    /// <param name="pointSize">The point size used for layout.</param>
    /// <param name="dpi">The resolution used to convert placeholder pixels into layout units.</param>
    /// <param name="scaleFactor">The font scale factor used by glyph layout.</param>
    /// <returns>The measurement expressed in synthetic font units.</returns>
    private static ushort ToGlyphUnits(float pixels, float pointSize, float dpi, float scaleFactor)
        => (ushort)MathF.Round(ToGlyphUnitsFloat(pixels, pointSize, dpi, scaleFactor));

    /// <summary>
    /// Converts a placeholder pixel measurement into fractional glyph units for bounds placement.
    /// </summary>
    /// <param name="pixels">The placeholder measurement in pixels.</param>
    /// <param name="pointSize">The point size used for layout.</param>
    /// <param name="dpi">The resolution used to convert placeholder pixels into layout units.</param>
    /// <param name="scaleFactor">The font scale factor used by glyph layout.</param>
    /// <returns>The measurement expressed in synthetic font units.</returns>
    private static float ToGlyphUnitsFloat(float pixels, float pointSize, float dpi, float scaleFactor)
        => pixels * scaleFactor / (pointSize * dpi);
}
