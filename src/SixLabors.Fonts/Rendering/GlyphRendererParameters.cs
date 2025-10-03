// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Globalization;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// The combined set of properties that uniquely identify the glyph that is to be rendered
/// at a particular size and dpi.
/// </summary>
[DebuggerDisplay("GlyphId = {GlyphId}, CodePoint = {CodePoint}, PointSize = {PointSize}, Dpi = {Dpi}")]
public readonly struct GlyphRendererParameters : IEquatable<GlyphRendererParameters>
{
    internal GlyphRendererParameters(
        GlyphMetrics metrics,
        TextRun textRun,
        float pointSize,
        float dpi,
        GlyphLayoutMode layoutMode,
        int graphemeIndex)
    {
        this.Font = metrics.FontMetrics.Description.FontNameInvariantCulture?.ToUpper(CultureInfo.InvariantCulture) ?? string.Empty;
        this.FontStyle = metrics.FontMetrics.Description.Style;
        this.GlyphId = metrics.GlyphId;
        this.GraphemeIndex = graphemeIndex;
        this.PointSize = pointSize;
        this.Dpi = dpi;
        this.GlyphType = metrics.GlyphType;
        this.GlyphColor = metrics.GlyphColor ?? default;
        this.TextRun = textRun;
        this.CodePoint = metrics.CodePoint;
        this.LayoutMode = layoutMode;
    }

    /// <summary>
    /// Gets the name of the Font this glyph belongs to.
    /// </summary>
    public string Font { get; }

    /// <summary>
    /// Gets the color details of this glyph.
    /// </summary>
    public GlyphColor GlyphColor { get; }

    /// <summary>
    /// Gets the type of this glyph.
    /// </summary>
    public GlyphType GlyphType { get; }

    /// <summary>
    /// Gets the style of the font this glyph belongs to.
    /// </summary>
    public FontStyle FontStyle { get; }

    /// <summary>
    /// Gets the id of the glyph within the font tables.
    /// </summary>
    public ushort GlyphId { get; }

    /// <summary>
    /// Gets the id of the composite glyph if the <see cref="GlyphType"/> is <see cref="GlyphType.Layer"/>;
    /// </summary>
    public ushort CompositeGlyphId { get; }

    /// <summary>
    /// Gets the index of the grapheme this glyph belongs to.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the codepoint represented by this glyph.
    /// </summary>
    public CodePoint CodePoint { get; }

    /// <summary>
    /// Gets the rendered point size.
    /// </summary>
    public float PointSize { get; }

    /// <summary>
    /// Gets the dots-per-inch the glyph is to be rendered at.
    /// </summary>
    public float Dpi { get; }

    /// <summary>
    /// Gets the layout mode applied to the glyph.
    /// </summary>
    public GlyphLayoutMode LayoutMode { get; }

    /// <summary>
    /// Gets the text run that this glyph belongs to.
    /// </summary>
    public TextRun TextRun { get; }

    /// <summary>
    /// Compares two <see cref="GlyphRendererParameters"/> objects for equality.
    /// </summary>
    /// <param name="left">
    /// The <see cref="GlyphRendererParameters"/> on the left side of the operand.
    /// </param>
    /// <param name="right">
    /// The <see cref="GlyphRendererParameters"/> on the right side of the operand.
    /// </param>
    /// <returns>
    /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    public static bool operator ==(GlyphRendererParameters left, GlyphRendererParameters right)
        => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="GlyphRendererParameters"/> objects for inequality.
    /// </summary>
    /// <param name="left">
    /// The <see cref="GlyphRendererParameters"/> on the left side of the operand.
    /// </param>
    /// <param name="right">
    /// The <see cref="GlyphRendererParameters"/> on the right side of the operand.
    /// </param>
    /// <returns>
    /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    public static bool operator !=(GlyphRendererParameters left, GlyphRendererParameters right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(GlyphRendererParameters other)
        => other.PointSize == this.PointSize
        && other.FontStyle == this.FontStyle
        && other.Dpi == this.Dpi
        && other.GlyphId == this.GlyphId
        && other.CompositeGlyphId == this.CompositeGlyphId
        && this.GraphemeIndex == other.GraphemeIndex
        && other.GlyphType == this.GlyphType
        && other.TextRun.TextAttributes == this.TextRun.TextAttributes
        && other.TextRun.TextDecorations == this.TextRun.TextDecorations
        && other.LayoutMode == this.LayoutMode
        && other.GlyphColor.Equals(this.GlyphColor)
        && ((other.Font is null && this.Font is null)
        || (other.Font?.Equals(this.Font, StringComparison.OrdinalIgnoreCase) == true));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is GlyphRendererParameters parameters && this.Equals(parameters);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        int a = HashCode.Combine(
            this.Font,
            this.PointSize,
            this.GlyphId,
            this.GlyphType,
            this.GlyphColor);

        int b = HashCode.Combine(
            this.FontStyle,
            this.Dpi,
            this.TextRun.TextAttributes,
            this.TextRun.TextDecorations,
            this.LayoutMode);

        int c = HashCode.Combine(
            this.CompositeGlyphId,
            this.GraphemeIndex);

        return HashCode.Combine(a, b, c);
    }
}
