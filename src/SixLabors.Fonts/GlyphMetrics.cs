// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a glyph metric from a particular font face.
/// </summary>
public abstract class GlyphMetrics
{
    private static readonly Vector2 YInverter = new(1, -1);

    internal GlyphMetrics(
        StreamFontMetrics font,
        ushort glyphId,
        CodePoint codePoint,
        Bounds bounds,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        GlyphType glyphType = GlyphType.Standard,
        GlyphColor? glyphColor = null)
    {
        this.FontMetrics = font;
        this.GlyphId = glyphId;
        this.CodePoint = codePoint;
        this.Bounds = bounds;
        this.Width = bounds.Max.X - bounds.Min.X;
        this.Height = bounds.Max.Y - bounds.Min.Y;
        this.UnitsPerEm = unitsPerEM;
        this.AdvanceWidth = advanceWidth;
        this.AdvanceHeight = advanceHeight;
        this.LeftSideBearing = leftSideBearing;
        this.RightSideBearing = (short)(this.AdvanceWidth - this.LeftSideBearing - this.Width);
        this.TopSideBearing = topSideBearing;
        this.BottomSideBearing = (short)(this.AdvanceHeight - this.TopSideBearing - this.Height);
        this.TextAttributes = textAttributes;
        this.TextDecorations = textDecorations;
        this.GlyphType = glyphType;
        this.GlyphColor = glyphColor;

        Vector2 offset = Vector2.Zero;
        Vector2 scaleFactor = new(unitsPerEM * 72F);
        if (textAttributes.HasFlag(TextAttributes.Subscript))
        {
            float units = this.UnitsPerEm;
            scaleFactor /= new Vector2(font.SubscriptXSize / units, font.SubscriptYSize / units);
            offset = new(font.SubscriptXOffset, font.SubscriptYOffset < 0 ? font.SubscriptYOffset : -font.SubscriptYOffset);
        }
        else if (textAttributes.HasFlag(TextAttributes.Superscript))
        {
            float units = this.UnitsPerEm;
            scaleFactor /= new Vector2(font.SuperscriptXSize / units, font.SuperscriptYSize / units);
            offset = new(font.SuperscriptXOffset, font.SuperscriptYOffset < 0 ? -font.SuperscriptYOffset : font.SuperscriptYOffset);
        }

        this.ScaleFactor = scaleFactor;
        this.Offset = offset;
    }

    internal GlyphMetrics(
        StreamFontMetrics font,
        ushort glyphId,
        CodePoint codePoint,
        Bounds bounds,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        Vector2 offset,
        Vector2 scaleFactor,
        TextRun textRun,
        GlyphType glyphType = GlyphType.Standard,
        GlyphColor? glyphColor = null)
    {
        // This is used during cloning. Ensure anything that could be changed is copied.
        this.FontMetrics = font;
        this.GlyphId = glyphId;
        this.CodePoint = codePoint;
        this.Bounds = new Bounds(bounds.Min, bounds.Max);
        this.Width = bounds.Max.X - bounds.Min.X;
        this.Height = bounds.Max.Y - bounds.Min.Y;
        this.UnitsPerEm = unitsPerEM;
        this.AdvanceWidth = advanceWidth;
        this.AdvanceHeight = advanceHeight;
        this.LeftSideBearing = leftSideBearing;
        this.RightSideBearing = (short)(this.AdvanceWidth - this.LeftSideBearing - this.Width);
        this.TopSideBearing = topSideBearing;
        this.BottomSideBearing = (short)(this.AdvanceHeight - this.TopSideBearing - this.Height);
        this.TextAttributes = textRun.TextAttributes;
        this.TextDecorations = textRun.TextDecorations;
        this.GlyphType = glyphType;
        this.GlyphColor = glyphColor;
        this.ScaleFactor = new Vector2(scaleFactor.X, scaleFactor.Y);
        this.Offset = new Vector2(offset.X, offset.Y);
        this.TextRun = textRun;
    }

    /// <summary>
    /// Gets the font metrics.
    /// </summary>
    internal StreamFontMetrics FontMetrics { get; }

    /// <summary>
    /// Gets the Unicode codepoint of the glyph.
    /// </summary>
    public CodePoint CodePoint { get; }

    /// <summary>
    /// Gets the advance width for horizontal layout, expressed in font units.
    /// </summary>
    public ushort AdvanceWidth { get; private set; }

    /// <summary>
    /// Gets the advance height for vertical layout, expressed in font units.
    /// </summary>
    public ushort AdvanceHeight { get; private set; }

    /// <summary>
    /// Gets the left side bearing for horizontal layout, expressed in font units.
    /// </summary>
    public short LeftSideBearing { get; }

    /// <summary>
    /// Gets the right side bearing for horizontal layout, expressed in font units.
    /// </summary>
    public short RightSideBearing { get; }

    /// <summary>
    /// Gets the top side bearing for vertical layout, expressed in font units.
    /// </summary>
    public short TopSideBearing { get; }

    /// <summary>
    /// Gets the bottom side bearing for vertical layout, expressed in font units.
    /// </summary>
    public short BottomSideBearing { get; }

    /// <summary>
    /// Gets the bounds, expressed in font units.
    /// </summary>
    internal Bounds Bounds { get; }

    /// <summary>
    /// Gets the width, expressed in font units.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height, expressed in font units.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Gets the glyph type.
    /// </summary>
    public GlyphType GlyphType { get; }

    /// <summary>
    /// Gets the color of this glyph when the <see cref="GlyphType"/> is <see cref="GlyphType.ColrLayer"/>
    /// </summary>
    public GlyphColor? GlyphColor { get; }

    /// <inheritdoc cref="FontMetrics.UnitsPerEm"/>
    public ushort UnitsPerEm { get; }

    /// <summary>
    /// Gets the id of the glyph within the font tables.
    /// </summary>
    public ushort GlyphId { get; }

    /// <summary>
    /// Gets the scale factor that is applied to all glyphs in this face.
    /// Normally calculated as 72 * <see cref="UnitsPerEm"/> so that 1pt = 1px
    /// unless the glyph has <see cref="TextAttributes"/> that apply scaling adjustment.
    /// </summary>
    public Vector2 ScaleFactor { get; }

    /// <summary>
    /// Gets or sets the offset in font design units.
    /// </summary>
    internal Vector2 Offset { get; set; }

    /// <summary>
    /// Gets the text run that the glyph belongs to.
    /// </summary>
    internal TextRun TextRun { get; } = null!;

    /// <summary>
    /// Gets the text attributes applied to the glyph.
    /// </summary>
    public TextAttributes TextAttributes { get; }

    /// <summary>
    /// Gets the text decorations applied to the glyph.
    /// </summary>
    public TextDecorations TextDecorations { get; }

    /// <summary>
    /// Performs a semi-deep clone (FontMetrics are not cloned) for rendering
    /// This allows caching the original in the font metrics.
    /// </summary>
    /// <param name="textRun">The current text run this glyph belongs to.</param>
    /// <returns>The new <see cref="GlyphMetrics"/>.</returns>
    internal abstract GlyphMetrics CloneForRendering(TextRun textRun);

    /// <summary>
    /// Apply an offset to the glyph.
    /// </summary>
    /// <param name="x">The x-offset.</param>
    /// <param name="y">The y-offset.</param>
    internal void ApplyOffset(short x, short y)
        => this.Offset = Vector2.Transform(this.Offset, Matrix3x2.CreateTranslation(x, y));

    /// <summary>
    /// Applies an advance to the glyph.
    /// </summary>
    /// <param name="x">The x-advance.</param>
    /// <param name="y">The y-advance.</param>
    internal void ApplyAdvance(short x, short y)
    {
        this.AdvanceWidth = (ushort)(this.AdvanceWidth + x);

        // AdvanceHeight values grow downward but font-space grows upward, hence negation
        this.AdvanceHeight = (ushort)(this.AdvanceHeight - y);
    }

    /// <summary>
    /// Sets a new advance width.
    /// </summary>
    /// <param name="x">The x-advance.</param>
    internal void SetAdvanceWidth(ushort x) => this.AdvanceWidth = x;

    /// <summary>
    /// Sets a new advance height.
    /// </summary>
    /// <param name="y">The y-advance.</param>
    internal void SetAdvanceHeight(ushort y) => this.AdvanceHeight = y;

    internal FontRectangle GetBoundingBox(GlyphLayoutMode mode, Vector2 origin, float scaledPointSize)
    {
        Vector2 scale = new Vector2(scaledPointSize) / this.ScaleFactor;
        Bounds bounds = this.Bounds;

        if (bounds.Equals(Bounds.Empty))
        {
            // For non-vertical layout, the advance width only is used to compute the bounding box
            // as the advance height represents the maximum possible advance.
            if (mode != GlyphLayoutMode.Vertical)
            {
                bounds = new Bounds(0, 0, this.AdvanceWidth, 0);
            }
            else
            {
                bounds = new Bounds(0, 0, 0, this.AdvanceHeight);
            }
        }

        // Rotate if required.
        if (mode == GlyphLayoutMode.VerticalRotated)
        {
            bounds = Bounds.Transform(in bounds, Matrix3x2.CreateRotation(-MathF.PI / 2F));
        }

        Vector2 size = bounds.Size() * scale;
        Vector2 location = (new Vector2(bounds.Min.X, bounds.Min.Y) + this.Offset) * scale * YInverter;

        location -= new Vector2(0, size.Y);
        location += origin;
        return new FontRectangle(location.X, location.Y, size.X, size.Y);
    }

    /// <summary>
    /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
    /// </summary>
    /// <param name="renderer">The surface renderer.</param>
    /// <param name="location">The location representing offset of the glyph outer bounds relative to the origin.</param>
    /// <param name="offset">The offset of the glyph vector relative to the top-left position of the glyph advance.</param>
    /// <param name="mode">The glyph layout mode to render using.</param>
    /// <param name="options">The options used to influence the rendering of this glyph.</param>
    internal abstract void RenderTo(IGlyphRenderer renderer, Vector2 location, Vector2 offset, GlyphLayoutMode mode, TextOptions options);

    internal void RenderDecorationsTo(IGlyphRenderer renderer, Vector2 location, GlyphLayoutMode mode, Matrix3x2 transform, float scaledPPEM)
    {
        bool isVerticalLayout = mode is GlyphLayoutMode.Vertical or GlyphLayoutMode.VerticalRotated;
        (Vector2 Start, Vector2 End, float Thickness) GetEnds(TextDecorations decorations, float thickness, float decoratorPosition)
        {
            // For vertical layout we need to draw a vertical line.
            if (isVerticalLayout)
            {
                float length = mode == GlyphLayoutMode.VerticalRotated ? this.AdvanceWidth : this.AdvanceHeight;
                if (length == 0)
                {
                    return (Vector2.Zero, Vector2.Zero, 0);
                }

                Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;

                // Undo the vertical offset applied when laying out the text.
                Vector2 scaledOffset = (this.Offset + new Vector2(decoratorPosition, 0)) * scale;

                length *= scale.Y;
                thickness *= scale.X;

                Vector2 tl = new(scaledOffset.X, scaledOffset.Y);
                Vector2 tr = new(scaledOffset.X + thickness, scaledOffset.Y);
                Vector2 bl = new(scaledOffset.X, scaledOffset.Y + length);

                thickness = tr.X - tl.X;

                // Horizontally offset the line to the correct horizontal position
                // based upon which side drawing occurs of the line.
                float m = decorations switch
                {
                    TextDecorations.Strikeout => .5F,
                    TextDecorations.Overline => 3,
                    _ => 1,
                };

                // Account for any future pixel clamping.
                scaledOffset = new Vector2(thickness * m, 0) + location;
                tl += scaledOffset;
                bl += scaledOffset;

                return (tl, bl, thickness);
            }
            else
            {
                float length = this.AdvanceWidth;
                if (length == 0)
                {
                    return (Vector2.Zero, Vector2.Zero, 0);
                }

                Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
                Vector2 scaledOffset = (this.Offset + new Vector2(0, decoratorPosition)) * scale;

                length *= scale.X;
                thickness *= scale.Y;

                Vector2 tl = new(scaledOffset.X, scaledOffset.Y);
                Vector2 tr = new(scaledOffset.X + length, scaledOffset.Y);
                Vector2 bl = new(scaledOffset.X, scaledOffset.Y + thickness);

                thickness = bl.Y - tl.Y;
                tl = (Vector2.Transform(tl, transform) * YInverter) + location;
                tr = (Vector2.Transform(tr, transform) * YInverter) + location;

                return (tl, tr, thickness);
            }
        }

        void SetDecoration(TextDecorations decorations, float thickness, float position)
        {
            (Vector2 start, Vector2 end, float calcThickness) = GetEnds(decorations, thickness, position);
            if (calcThickness != 0)
            {
                renderer.SetDecoration(decorations, start, end, calcThickness);
            }
        }

        // Allow the renderer to override the decorations to attach.
        // When rendering glyphs vertically we use synthesized positions based upon comparisons with Pango/browsers.
        // We deviate from browsers in a few ways:
        // - When rendering rotated glyphs and use the default values because it fits the glyphs better.
        // - We include the adjusted scale for subscript and superscript glyphs.
        // - We make no attempt to adjust the underline position along a text line to render at the same position.
        TextDecorations decorations = renderer.EnabledDecorations();
        bool synthesized = mode == GlyphLayoutMode.Vertical;
        if ((decorations & TextDecorations.Underline) == TextDecorations.Underline)
        {
            SetDecoration(TextDecorations.Underline, this.FontMetrics.UnderlineThickness, synthesized ? Math.Abs(this.FontMetrics.UnderlinePosition) : this.FontMetrics.UnderlinePosition);
        }

        if ((decorations & TextDecorations.Strikeout) == TextDecorations.Strikeout)
        {
            SetDecoration(TextDecorations.Strikeout, this.FontMetrics.StrikeoutSize, synthesized ? this.FontMetrics.UnitsPerEm * .5F : this.FontMetrics.StrikeoutPosition);
        }

        if ((decorations & TextDecorations.Overline) == TextDecorations.Overline)
        {
            // There's no built in metrics for overline thickness so use underline.
            SetDecoration(TextDecorations.Overline, this.FontMetrics.UnderlineThickness, this.UnitsPerEm - this.FontMetrics.UnderlinePosition);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the specified code point should be skipped when rendering.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The <see cref="bool"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected internal static bool ShouldSkipGlyphRendering(CodePoint codePoint)
        => UnicodeUtility.IsDefaultIgnorableCodePoint((uint)codePoint.Value) && !UnicodeUtility.ShouldRenderWhiteSpaceOnly(codePoint);

    /// <summary>
    /// Returns the size to render/measure the glyph based on the given size and resolution in px units.
    /// </summary>
    /// <param name="pointSize">The font size in pt units.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
    /// <returns>The <see cref="float"/>.</returns>
    internal float GetScaledSize(float pointSize, float dpi)
    {
        float scaledPPEM = dpi * pointSize;
        bool forcePPEMToInt = (this.FontMetrics.HeadFlags & HeadTable.HeadFlags.ForcePPEMToInt) != 0;

        if (forcePPEMToInt)
        {
            scaledPPEM = MathF.Round(scaledPPEM);
        }

        return scaledPPEM;
    }

    /// <summary>
    /// Gets the rotation matrix for the glyph based on the layout mode.
    /// </summary>
    /// <param name="mode">The glyph layout mode.</param>
    /// <returns>The<see cref="bool"/>.</returns>
    internal static Matrix3x2 GetRotationMatrix(GlyphLayoutMode mode)
    {
        if (mode == GlyphLayoutMode.VerticalRotated)
        {
            // Rotate 90 degrees clockwise.
            return Matrix3x2.CreateRotation(-MathF.PI / 2F);
        }

        return Matrix3x2.Identity;
    }
}
