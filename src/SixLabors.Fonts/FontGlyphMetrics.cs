// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a glyph metric from a particular font face.
/// </summary>
public abstract class FontGlyphMetrics
{
    /// <summary>
    /// The number of typographic points per inch. Scaled pixels-per-em values are computed as
    /// point size multiplied by dpi, so dividing by this constant converts them to the em size
    /// in device pixels.
    /// </summary>
    private const float PointsPerInch = 72F;

    /// <summary>
    /// Negates the y-axis to convert between the y-up font coordinate space and the y-down
    /// device coordinate space.
    /// </summary>
    private static readonly Vector2 YInverter = new(1, -1);

    /// <summary>
    /// The horizontal shear applied to synthesize an oblique (faux italic) slant when an italic
    /// style is requested but the resolved font face provides no italic face. Chromium and
    /// SkParagraph use an exact quarter shear, which produces an angle of approximately 14.036
    /// degrees.
    /// </summary>
    private const float SyntheticObliqueSkew = 1F / 4F;

    /// <summary>
    /// The outline strength, expressed as a fraction of the em size, applied to synthesize a bold
    /// (faux bold) weight when a bold style is requested but the resolved font face provides no
    /// bold face. The divisor of 31 retains FreeType's dilation behavior while matching the
    /// synthesized weight produced by Chromium's DirectWrite backend more closely than desktop
    /// FreeType's divisor of 24 or Skia Android's divisor of 34.
    /// </summary>
    private const float SyntheticBoldEmScale = 1F / 31F;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontGlyphMetrics"/> class, deriving the
    /// scale factor and offset from the text attributes: subscript and superscript glyphs
    /// receive the font's configured scaling and baseline displacement. All glyphs loaded
    /// from font data use this form.
    /// </summary>
    /// <param name="font">The font metrics this glyph belongs to.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point for this glyph.</param>
    /// <param name="bounds">The glyph bounds in font design units.</param>
    /// <param name="advanceWidth">The advance width in font design units.</param>
    /// <param name="advanceHeight">The advance height in font design units.</param>
    /// <param name="leftSideBearing">The left side bearing in font design units.</param>
    /// <param name="topSideBearing">The top side bearing in font design units.</param>
    /// <param name="unitsPerEM">The units per em for the font.</param>
    /// <param name="textAttributes">The text attributes applied to the glyph.</param>
    /// <param name="textDecorations">The text decorations applied to the glyph.</param>
    /// <param name="glyphType">The glyph type.</param>
    internal FontGlyphMetrics(
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
        GlyphType glyphType)
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

        Vector2 offset = Vector2.Zero;

        // Dividing a scaled pixels-per-em value (point size * dpi) by this factor yields the
        // device pixels per font unit.
        Vector2 scaleFactor = new(unitsPerEM * PointsPerInch);

        if ((textAttributes & TextAttributes.Subscript) == TextAttributes.Subscript)
        {
            float units = this.UnitsPerEm;
            scaleFactor /= new Vector2(font.SubscriptXSize / units, font.SubscriptYSize / units);
            offset = new Vector2(font.SubscriptXOffset, font.SubscriptYOffset < 0 ? font.SubscriptYOffset : -font.SubscriptYOffset);
        }
        else if ((textAttributes & TextAttributes.Superscript) == TextAttributes.Superscript)
        {
            float units = this.UnitsPerEm;
            scaleFactor /= new Vector2(font.SuperscriptXSize / units, font.SuperscriptYSize / units);
            offset = new Vector2(font.SuperscriptXOffset, font.SuperscriptYOffset < 0 ? -font.SuperscriptYOffset : font.SuperscriptYOffset);
        }

        this.ScaleFactor = scaleFactor;
        this.Offset = offset;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontGlyphMetrics"/> class taking the
    /// scale factor and offset verbatim. Used by synthetic metrics such as placeholders,
    /// whose dimensions are authored against the plain font scale factor and must not
    /// receive attribute-derived scaling.
    /// </summary>
    /// <param name="font">The font metrics this glyph belongs to.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point for this glyph.</param>
    /// <param name="bounds">The glyph bounds in font design units.</param>
    /// <param name="advanceWidth">The advance width in font design units.</param>
    /// <param name="advanceHeight">The advance height in font design units.</param>
    /// <param name="leftSideBearing">The left side bearing in font design units.</param>
    /// <param name="topSideBearing">The top side bearing in font design units.</param>
    /// <param name="unitsPerEM">The units per em for the font.</param>
    /// <param name="offset">The offset in font design units.</param>
    /// <param name="scaleFactor">The scale factor. Dividing a scaled pixels-per-em value by it yields device pixels per font unit.</param>
    /// <param name="textAttributes">The text attributes applied to the glyph.</param>
    /// <param name="textDecorations">The text decorations applied to the glyph.</param>
    /// <param name="glyphType">The glyph type.</param>
    internal FontGlyphMetrics(
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
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        GlyphType glyphType)
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
        this.ScaleFactor = scaleFactor;
        this.Offset = offset;
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
    public ushort AdvanceWidth { get; }

    /// <summary>
    /// Gets the advance height for vertical layout, expressed in font units.
    /// </summary>
    public ushort AdvanceHeight { get; }

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
    /// Gets the color palette selection the glyph's colors were resolved with, or
    /// <see langword="null"/> when the glyph resolves no palette colors. Painted formats
    /// override this so renderer caches can distinguish palette variants of one glyph.
    /// </summary>
    internal virtual FontPalette? FontPalette => null;

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
    /// Gets the offset in font design units.
    /// </summary>
    internal Vector2 Offset { get; }

    /// <summary>
    /// Gets the text attributes applied to the glyph.
    /// </summary>
    public TextAttributes TextAttributes { get; }

    /// <summary>
    /// Gets the text decorations applied to the glyph.
    /// </summary>
    public TextDecorations TextDecorations { get; }

    /// <summary>
    /// Gets the horizontal shear factor used to synthesize an oblique (faux italic) slant for
    /// this glyph. A non-zero value is returned only when the associated text run requests an
    /// italic style that the resolved font face does not itself provide, mirroring the CSS
    /// <c>font-synthesis: style</c> behavior used by web browsers.
    /// </summary>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <returns>
    /// The shear factor to apply in the glyph's Y-up coordinate space, or <c>0</c> when no
    /// synthesis is required.
    /// </returns>
    internal float GetObliqueSkew(TextRun? textRun)
    {
        Font? font = textRun?.ResolvedFont;
        if (font is null)
        {
            return 0F;
        }

        bool requestedItalic = (font.RequestedStyle & FontStyle.Italic) == FontStyle.Italic;
        bool resolvedItalic = (this.FontMetrics.Description.Style & FontStyle.Italic) == FontStyle.Italic;
        return requestedItalic && !resolvedItalic ? SyntheticObliqueSkew : 0F;
    }

    /// <summary>
    /// Creates a horizontal shear matrix for the given oblique skew factor, expressed in the
    /// glyph's Y-up coordinate space.
    /// </summary>
    /// <param name="skew">The horizontal shear factor.</param>
    /// <returns>The shear <see cref="Matrix3x2"/>.</returns>
    internal static Matrix3x2 CreateObliqueMatrix(float skew)
    {
        Matrix3x2 matrix = Matrix3x2.Identity;
        matrix.M21 = skew;
        return matrix;
    }

    /// <summary>
    /// Gets a value indicating whether a heavier weight must be synthesized for this glyph.
    /// Variable fonts apply their registered weight axis instead.
    /// </summary>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <returns><c>true</c> if bold synthesis is required; otherwise <c>false</c>.</returns>
    internal bool ShouldSynthesizeBold(TextRun? textRun)
    {
        // Chromium deliberately leaves color emoji at their authored weight. Applying outline
        // dilation to a painted glyph would also distort each independently colored layer.
        if (this.GlyphType == GlyphType.Painted)
        {
            return false;
        }

        Font? font = textRun?.ResolvedFont;
        if (font is null || (textRun!.UsesVariableWeight && ReferenceEquals(font.FontMetrics, this.FontMetrics)))
        {
            return false;
        }

        FontWeight? requestedWeight = textRun.ResolvedFontWeight;
        if (!requestedWeight.HasValue && (font.RequestedStyle & FontStyle.Bold) == FontStyle.Bold)
        {
            requestedWeight = FontWeight.Bold;
        }

        if (!requestedWeight.HasValue)
        {
            return false;
        }

        // CSS weight values select a face; they do not represent proportional stroke strength.
        // Chromium treats 600 as the boundary between regular and bold, then applies one fixed
        // synthetic-bold operation when the request and resolved static face lie on opposite
        // sides of that boundary. Thus 600, 700, 800, and 900 receive identical dilation.
        return (int)requestedWeight.Value >= (int)FontWeight.SemiBold
            && (int)this.FontMetrics.Description.Weight < (int)FontWeight.SemiBold;
    }

    /// <summary>
    /// Gets the total x/y outline strength, in pixels, used to synthesize a bold (faux bold) weight
    /// for this glyph, or <c>0</c> when no synthesis is required. The FreeType dilation algorithm
    /// halves this value internally before moving points along their lateral bisectors.
    /// </summary>
    /// <param name="scaledPointSize">The scaled point size, mapped to pixels by the caller.</param>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <returns>The emboldening strength in pixels.</returns>
    internal float GetSyntheticBoldStrength(float scaledPointSize, TextRun? textRun)
        => this.ShouldSynthesizeBold(textRun) ? SyntheticBoldEmScale * this.UnitsPerEm * (scaledPointSize / this.ScaleFactor.X) : 0F;

    /// <summary>
    /// Calculates the glyph bounding box in device-space (Y-down) coordinates,
    /// given the layout mode, render origin, and scaled point size.
    /// </summary>
    /// <remarks>
    /// Steps:
    /// 1) Select glyph bounds (or synthesize from advances if empty).
    /// 2) Account for synthetic weight, oblique shear, and layout rotation.
    /// 3) Convert from Y-up to Y-down coordinates.
    /// 4) Scale and translate to device space using the specified origin.
    /// </remarks>
    /// <param name="mode">The glyph layout mode (horizontal, vertical, or vertical rotated).</param>
    /// <param name="origin">The render-space origin in pixels.</param>
    /// <param name="scaledPointSize">The scaled point size, mapped to pixels by the caller.</param>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <param name="positionOffset">The positioned placement offset in font design units, composed with the glyph's base offset.</param>
    /// <param name="positionedAdvance">The positioned advance in font design units used to substitute bounds for glyphs without an outline.</param>
    /// <returns>
    /// A <see cref="FontRectangle"/> representing the glyph bounds in device space.
    /// </returns>
    internal FontRectangle GetBoundingBox(GlyphLayoutMode mode, Vector2 origin, float scaledPointSize, TextRun? textRun, Vector2 positionOffset, Vector2 positionedAdvance)
    {
        Vector2 scale = new(scaledPointSize / this.ScaleFactor.X, scaledPointSize / this.ScaleFactor.Y);
        Bounds b = this.GetDesignBounds();

        // 1) Substitute fallback bounds if the glyph has no outline. The box uses the
        // positioned advance rather than the font's nominal advance because it stands
        // for the space the glyph occupies in the laid-out line: positioning can
        // rewrite the advance, and an invisible default ignorable is deliberately
        // zeroed, so its nominal advance would claim ink no renderer produces.
        // Callers without positioned state pass the nominal metrics advances, which
        // are identical whenever nothing repositioned the glyph.
        if (b.Equals(Bounds.Empty))
        {
            if (mode == GlyphLayoutMode.Vertical)
            {
                // For vertical layout, set Y-up min = -advance to 0 so Y-down is 0..+advance.
                b = new Bounds(0f, -positionedAdvance.Y, 0f, 0f);
            }
            else
            {
                // For horizontal layout, just use the positioned advance width.
                b = new Bounds(0f, 0f, positionedAdvance.X, 0f);
            }
        }

        // 2) Apply synthetic weight and the shared outline transform. The positioned
        // offset composes with any base metric offset in the same Y-up space.
        Vector2 offsetUp = this.Offset + positionOffset;

        // FreeType halves the supplied strength before moving points. Inflating both sides by that
        // half-strength preserves the nominal increase in width and height for measurement while
        // the renderer applies the direction-sensitive point dilation.
        if (this.ShouldSynthesizeBold(textRun))
        {
            float inflate = SyntheticBoldEmScale * this.UnitsPerEm * .5F;
            b = new Bounds(b.Min - new Vector2(inflate), b.Max + new Vector2(inflate));
        }

        Matrix3x2 outlineTransform = this.GetOutlineTransform(mode, textRun);
        b = Bounds.Transform(in b, outlineTransform);
        offsetUp = Vector2.Transform(offsetUp, outlineTransform);

        // 3) Flip Y to convert to device-space (Y-down).
        Vector2 minDown = b.Min * YInverter;
        Vector2 maxDown = b.Max * YInverter;
        Vector2 offsetDown = offsetUp * YInverter;

        // Normalize bounds after flipping.
        float minX = MathF.Min(minDown.X, maxDown.X);
        float maxX = MathF.Max(minDown.X, maxDown.X);
        float minY = MathF.Min(minDown.Y, maxDown.Y);
        float maxY = MathF.Max(minDown.Y, maxDown.Y);

        // 4) Apply scaling and origin translation.
        Vector2 size = new(maxX - minX, maxY - minY);
        size *= scale;
        Vector2 location = origin + ((new Vector2(minX, minY) + offsetDown) * scale);

        return new FontRectangle(location.X, location.Y, size.X, size.Y);
    }

    /// <summary>
    /// Gets the design-space bounds used to measure this glyph's rendered geometry.
    /// </summary>
    /// <returns>The design-space bounds.</returns>
    internal virtual Bounds GetDesignBounds() => this.Bounds;

    /// <summary>
    /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
    /// </summary>
    /// <remarks>
    /// The rendering sequence for a glyph is: <see cref="IGlyphRenderer.BeginGlyph"/>, the
    /// outline geometry via <see cref="RenderOutlineTo"/>, <see cref="IGlyphRenderer.EndGlyph"/>,
    /// then any text decorations via <see cref="IGlyphRenderer.SetDecoration"/>. Decoration
    /// geometry is computed before the outline is emitted so that, when skip-ink applies, the
    /// outline stream can be observed as it is emitted rather than requiring a second decoding
    /// pass to locate the ink.
    /// </remarks>
    /// <param name="renderer">The surface renderer.</param>
    /// <param name="graphemeIndex">The index of the grapheme this glyph is part of.</param>
    /// <param name="glyphOrigin">The origin used to render the glyph outline.</param>
    /// <param name="decorationOrigin">The origin used to render text decorations.</param>
    /// <param name="layoutAdvance">
    /// The laid-out advance for the glyph in DPI-normalized layout units, including layout-time
    /// spacing such as tracking and justification. Negative components indicate no layout
    /// advance is available and the glyph's own metrics apply.
    /// </param>
    /// <param name="mode">The glyph layout mode to render using.</param>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <param name="positionOffset">The positioned placement offset in font design units, composed with the glyph's base offset.</param>
    /// <param name="positionedAdvance">The positioned advance in font design units used to substitute bounds for glyphs without an outline.</param>
    /// <param name="pointSize">The point size used to render this glyph.</param>
    /// <param name="dpi">The pixel density used to render this glyph.</param>
    /// <param name="hintingMode">The hinting mode used to render this glyph.</param>
    /// <param name="textDecorationSkipInk">The skip-ink behavior for underline and overline decorations.</param>
    /// <param name="decorationPositioningMode">The mode used to position text decorations.</param>
    /// <param name="decorationFontMetrics">The font metrics used to position text decorations.</param>
    internal virtual void RenderTo(
        IGlyphRenderer renderer,
        int graphemeIndex,
        Vector2 glyphOrigin,
        Vector2 decorationOrigin,
        Vector2 layoutAdvance,
        GlyphLayoutMode mode,
        TextRun textRun,
        Vector2 positionOffset,
        Vector2 positionedAdvance,
        float pointSize,
        float dpi,
        HintingMode hintingMode,
        TextDecorationSkipInk textDecorationSkipInk,
        DecorationPositioningMode decorationPositioningMode,
        FontMetrics decorationFontMetrics)
    {
        // https://www.unicode.org/faq/unsup_char.html
        if (ShouldSkipGlyphRendering(this.CodePoint))
        {
            return;
        }

        // Convert the DPI-normalized layout values to device pixels.
        glyphOrigin *= dpi;
        decorationOrigin *= dpi;
        layoutAdvance *= dpi;
        float scaledPPEM = this.GetScaledSize(pointSize, dpi, hintingMode);
        bool whitespace = UnicodeUtility.ShouldRenderWhiteSpaceOnly(this.CodePoint);

        // Fitted outlines and decorations share one device lattice, but their translations are
        // composed differently. The format-specific outline origin is resolved below with its
        // scaled metric and positioning offsets included. Decorations never flow through outline
        // emission, so their independent origin rounds here with the same per-mode axis policy:
        // full hinting on both axes, standard hinting on the glyph's hinted axis only, which a
        // rotated vertical glyph maps onto device X.
        HintingMode resolvedHinting = this.ResolveHintingMode(hintingMode);
        if (resolvedHinting == HintingMode.Full)
        {
            decorationOrigin = new Vector2(MathF.Floor(decorationOrigin.X + 0.5F), MathF.Floor(decorationOrigin.Y + 0.5F));
        }
        else if (resolvedHinting == HintingMode.Standard)
        {
            decorationOrigin = mode == GlyphLayoutMode.VerticalRotated
                ? new Vector2(MathF.Floor(decorationOrigin.X + 0.5F), decorationOrigin.Y)
                : new Vector2(decorationOrigin.X, MathF.Floor(decorationOrigin.Y + 0.5F));
        }

        // BeginGlyph may satisfy the glyph from a renderer-owned cache and decline outline
        // emission. Resolve the format-specific grid translation before that decision so the
        // reported metric bounds and a freshly emitted outline share one placement. Renderers
        // can then cache solely from the supplied bounds without reproducing hinting rules.
        if (!whitespace)
        {
            glyphOrigin = this.ResolveOutlineOrigin(glyphOrigin, mode, textRun, positionOffset, scaledPPEM, resolvedHinting);
        }

        Matrix3x2 rotation = GetRotationMatrix(mode);
        FontRectangle box = this.GetBoundingBox(mode, glyphOrigin, scaledPPEM, textRun, positionOffset, positionedAdvance);
        ClipBounds? clipBounds = this.GetClipBounds(glyphOrigin, mode, textRun, positionOffset, scaledPPEM);
        GlyphRendererParameters parameters = new(this, textRun, pointSize, dpi, mode, graphemeIndex, hintingMode, clipBounds);

        if (!renderer.BeginGlyph(in box, in parameters))
        {
            return;
        }

        bool isVerticalLayout = mode is GlyphLayoutMode.Vertical or GlyphLayoutMode.VerticalRotated;

        // Decoration geometry depends only on font metrics and scale, so it is computed
        // before the outline is emitted. This lets skip-ink observe the outline stream as it
        // is emitted instead of decoding the outline a second time afterwards.
        TextDecorationGeometry decorationGeometry = this.ComputeDecorationGeometry(
            renderer.EnabledDecorations(),
            textRun,
            decorationOrigin,
            mode,
            rotation,
            scaledPPEM,
            layoutAdvance,
            decorationPositioningMode,
            decorationFontMetrics);

        // When skip-ink applies and a decoration band can intersect the glyph's box, tee the
        // outline emission into per-band interval collectors. The collectors see exactly the
        // stream the renderer receives, so the measured ink matches the rendered geometry
        // (including hinting) at the cost of one extra call per outline segment. The tee and
        // collectors come from a per-thread scratch, so steady-state decorated rendering does
        // not allocate per glyph.
        // Whitespace never contributes ink; strikethrough always crosses ink by definition.
        GlyphIntersectionCollector? underlineInk = null;
        GlyphIntersectionCollector? overlineInk = null;
        SkipInkScratch? scratch = null;
        IGlyphRenderer outlineTarget = renderer;
        if (textDecorationSkipInk == TextDecorationSkipInk.Auto && !whitespace)
        {
            bool trackUnderline = TryGetInkBand(decorationGeometry.Underline, isVerticalLayout, in box, out float underlineBandStart, out float underlineBandEnd);
            bool trackOverline = TryGetInkBand(decorationGeometry.Overline, isVerticalLayout, in box, out float overlineBandStart, out float overlineBandEnd);
            if (trackUnderline || trackOverline)
            {
                scratch = SkipInkScratch.Rent();
                if (trackUnderline)
                {
                    underlineInk = scratch.UnderlineCollector;
                    underlineInk.Reset(underlineBandStart, underlineBandEnd, isVerticalLayout);
                }

                if (trackOverline)
                {
                    overlineInk = scratch.OverlineCollector;
                    overlineInk.Reset(overlineBandStart, overlineBandEnd, isVerticalLayout);
                }

                scratch.Tee.Reset(renderer, underlineInk, overlineInk);
                outlineTarget = scratch.Tee;
            }
        }

        try
        {
            if (!whitespace)
            {
                this.RenderOutlineTo(outlineTarget, glyphOrigin, mode, textRun, positionOffset, positionedAdvance, scaledPPEM, resolvedHinting);
            }

            renderer.EndGlyph();

            // Emit in the fixed underline, strikethrough, overline order relied upon by renderers.
            EmitDecoration(renderer, decorationGeometry.Underline, underlineInk);
            EmitDecoration(renderer, decorationGeometry.Strikeout, null);
            EmitDecoration(renderer, decorationGeometry.Overline, overlineInk);
        }
        finally
        {
            scratch?.Release();
        }
    }

    /// <summary>
    /// Gets the clip bounds for the glyph at the given placement, or <see langword="null"/>
    /// when the font defines none. Only painted glyph formats supply clip bounds.
    /// </summary>
    /// <param name="glyphOrigin">The origin used to render the glyph outline, in device pixels.</param>
    /// <param name="mode">The glyph layout mode to render using.</param>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <param name="positionOffset">The positioned placement offset in font design units, composed with the glyph's base offset.</param>
    /// <param name="scaledPPEM">The scaled pixels-per-em value used to scale the outline.</param>
    /// <returns>The clip bounds, or <see langword="null"/>.</returns>
    internal virtual ClipBounds? GetClipBounds(
        Vector2 glyphOrigin,
        GlyphLayoutMode mode,
        TextRun? textRun,
        Vector2 positionOffset,
        float scaledPPEM)
        => null;

    /// <summary>
    /// Renders only the glyph outline geometry to the specified renderer, without glyph or
    /// decoration bookkeeping, using the same transforms as
    /// <see cref="RenderTo(IGlyphRenderer, int, Vector2, Vector2, Vector2, GlyphLayoutMode, TextRun, Vector2, Vector2, float, float, HintingMode, TextDecorationSkipInk, DecorationPositioningMode, FontMetrics)"/>.
    /// </summary>
    /// <param name="renderer">The surface renderer.</param>
    /// <param name="glyphOrigin">The origin used to render the glyph outline, in device pixels.</param>
    /// <param name="mode">The glyph layout mode to render using.</param>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <param name="positionOffset">The positioned placement offset in font design units, composed with the glyph's base offset.</param>
    /// <param name="positionedAdvance">The positioned advance in font design units used to substitute bounds for glyphs without an outline.</param>
    /// <param name="scaledPPEM">The scaled pixels-per-em value used to scale the outline.</param>
    /// <param name="hintingMode">The hinting mode used to render the glyph.</param>
    internal virtual void RenderOutlineTo(
        IGlyphRenderer renderer,
        Vector2 glyphOrigin,
        GlyphLayoutMode mode,
        TextRun? textRun,
        Vector2 positionOffset,
        Vector2 positionedAdvance,
        float scaledPPEM,
        HintingMode hintingMode)
    {
    }

    /// <summary>
    /// Computes the positioned line geometry for the text decorations enabled on the current
    /// glyph: underline, strikeout, and overline.
    /// </summary>
    /// <remarks>When rendering in vertical layout modes, decoration positions are synthesized to match common
    /// typographic conventions. The renderer may override which decorations are enabled. Overline thickness is derived
    /// from underline metrics if not explicitly specified.</remarks>
    /// <param name="enabledDecorations">The decorations the renderer reports as enabled.</param>
    /// <param name="textRun">The text run styling this glyph, consulted for per-decoration geometry overrides.</param>
    /// <param name="location">The position, in device pixels, where the decorations should be rendered relative to the glyph.</param>
    /// <param name="mode">The layout mode that determines the orientation and positioning of the decorations (e.g., horizontal, vertical,
    /// or vertical rotated).</param>
    /// <param name="transform">The transformation matrix applied to the decoration coordinates before rendering.</param>
    /// <param name="scaledPPEM">The scaled pixels-per-em value used to adjust decoration size and positioning for the current rendering context.</param>
    /// <param name="layoutAdvance">
    /// The laid-out advance for the glyph in device pixels, including layout-time spacing such
    /// as tracking and justification. Negative components indicate no layout advance is
    /// available and the glyph's own metrics apply.
    /// </param>
    /// <param name="decorationPositioningMode">The mode used to position text decorations.</param>
    /// <param name="decorationFontMetrics">The font metrics used to position text decorations.</param>
    /// <returns>The positioned decoration lines; absent entries are disabled or degenerate.</returns>
    private TextDecorationGeometry ComputeDecorationGeometry(
        TextDecorations enabledDecorations,
        TextRun textRun,
        Vector2 location,
        GlyphLayoutMode mode,
        Matrix3x2 transform,
        float scaledPPEM,
        Vector2 layoutAdvance,
        DecorationPositioningMode decorationPositioningMode,
        FontMetrics decorationFontMetrics)
    {
        TextDecorationGeometry geometry = default;
        if (enabledDecorations == TextDecorations.None)
        {
            return geometry;
        }

        bool perGlyph = decorationPositioningMode == DecorationPositioningMode.GlyphFont;
        FontMetrics fontMetrics = perGlyph
            ? this.FontMetrics
            : decorationFontMetrics;

        // The scale factor for the decoration length is treated separately from other factors
        // as it is used to scale the length of the decoration line.
        // This must always be derived from the glyph's own scale factor to ensure correct length.
        Vector2 lengthScaleFactor = this.ScaleFactor;

        // These factors determine horizontal and vertical scaling and offset for the decorations.
        // and are either per-glyph or derived from the common font metrics.
        Vector2 scaleFactor;
        Vector2 offset;
        if (perGlyph)
        {
            // Use the pre-calculated values from this glyph.
            scaleFactor = this.ScaleFactor;
            offset = this.Offset;
        }
        else
        {
            // To ensure that we share the scaling when sharing font metrics we need to
            // recalculate the offset and scale factor here using the common font metrics.
            scaleFactor = new Vector2(fontMetrics.UnitsPerEm * PointsPerInch);
            offset = Vector2.Zero;
            if ((this.TextAttributes & TextAttributes.Subscript) == TextAttributes.Subscript)
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SubscriptXSize / units, fontMetrics.SubscriptYSize / units);
                offset = new Vector2(fontMetrics.SubscriptXOffset, fontMetrics.SubscriptYOffset < 0 ? fontMetrics.SubscriptYOffset : -fontMetrics.SubscriptYOffset);
            }
            else if ((this.TextAttributes & TextAttributes.Superscript) == TextAttributes.Superscript)
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SuperscriptXSize / units, fontMetrics.SuperscriptYSize / units);
                offset = new Vector2(fontMetrics.SuperscriptXOffset, fontMetrics.SuperscriptYOffset < 0 ? -fontMetrics.SuperscriptYOffset : fontMetrics.SuperscriptYOffset);
            }
        }

        bool isVerticalLayout = mode is GlyphLayoutMode.Vertical or GlyphLayoutMode.VerticalRotated;
        (Vector2 Start, Vector2 End, float Thickness) GetEnds(TextDecorations decorations, float thickness, float decoratorPosition)
        {
            // For vertical layout we need to draw a vertical line.
            if (isVerticalLayout)
            {
                float length = mode == GlyphLayoutMode.VerticalRotated ? this.AdvanceWidth : this.AdvanceHeight;

                Vector2 lengthScale = new Vector2(scaledPPEM) / lengthScaleFactor;
                Vector2 scale = new Vector2(scaledPPEM) / scaleFactor;

                // Undo the vertical offset applied when laying out the text.
                Vector2 scaledOffset = (offset + new Vector2(decoratorPosition, 0)) * scale;

                length *= lengthScale.Y;

                // Prefer the laid-out advance so decorations span layout-time spacing
                // such as tracking and justification.
                if (layoutAdvance.Y >= 0)
                {
                    length = layoutAdvance.Y;
                }

                if (length == 0)
                {
                    return (Vector2.Zero, Vector2.Zero, 0);
                }

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

                Vector2 lengthScale = new Vector2(scaledPPEM) / lengthScaleFactor;
                Vector2 scale = new Vector2(scaledPPEM) / scaleFactor;
                Vector2 scaledOffset = (offset + new Vector2(0, decoratorPosition)) * scale;

                length *= lengthScale.X;

                // Prefer the laid-out advance so decorations span layout-time spacing
                // such as tracking and justification.
                if (layoutAdvance.X >= 0)
                {
                    length = layoutAdvance.X;
                }

                if (length == 0)
                {
                    return (Vector2.Zero, Vector2.Zero, 0);
                }

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

        TextDecorationLine? Capture(TextDecorations decorations, float thickness, float position)
        {
            (Vector2 start, Vector2 end, float calcThickness) = GetEnds(decorations, thickness, position);
            if (calcThickness == 0)
            {
                return null;
            }

            // A run may override the metric-derived thickness (for example when it is drawn with an
            // explicit stroke width). The override is already in device pixels, so it replaces the
            // scaled metric thickness directly and flows on to the skip-ink band and gap clearance,
            // both of which key off the line thickness.
            if (textRun.GetDecorationOptions(decorations)?.Thickness is float overrideThickness)
            {
                calcThickness = overrideThickness;
            }

            return new TextDecorationLine(decorations, start, end, calcThickness);
        }

        // The enabled decorations come from the renderer so it can override the set to attach.
        // When rendering glyphs vertically we use synthesized positions based upon comparisons with Pango/browsers.
        // We deviate from browsers in a few ways:
        // - When rendering rotated glyphs and use the default values because it fits the glyphs better.
        // - We include the adjusted scale for subscript and superscript glyphs.
        // - We make no attempt to adjust the underline position along a text line to render at the same position.
        bool synthesized = mode == GlyphLayoutMode.Vertical;
        if ((enabledDecorations & TextDecorations.Underline) == TextDecorations.Underline)
        {
            geometry.Underline = Capture(TextDecorations.Underline, fontMetrics.UnderlineThickness, synthesized ? Math.Abs(fontMetrics.UnderlinePosition) : fontMetrics.UnderlinePosition);
        }

        if ((enabledDecorations & TextDecorations.Strikeout) == TextDecorations.Strikeout)
        {
            geometry.Strikeout = Capture(TextDecorations.Strikeout, fontMetrics.StrikeoutSize, synthesized ? fontMetrics.UnitsPerEm * .5F : fontMetrics.StrikeoutPosition);
        }

        if ((enabledDecorations & TextDecorations.Overline) == TextDecorations.Overline)
        {
            // There's no built in metrics for overline thickness so use underline.
            geometry.Overline = Capture(TextDecorations.Overline, fontMetrics.UnderlineThickness, fontMetrics.UnitsPerEm - fontMetrics.UnderlinePosition);
        }

        return geometry;
    }

    /// <summary>
    /// Computes the cross-axis interval covered by a decoration's drawn stroke. Renderers draw
    /// horizontal underlines below the decoration position and overlines above it; vertical
    /// positions are already offset to the drawn side when the geometry is computed, with
    /// underlines extending left of the position and overlines right.
    /// </summary>
    /// <param name="line">The positioned decoration line.</param>
    /// <param name="isVerticalLayout">Whether the decoration runs vertically, banding on x instead of y.</param>
    /// <returns>The band's start and end on the cross axis, in device pixels.</returns>
    private static (float Start, float End) GetDecorationBand(in TextDecorationLine line, bool isVerticalLayout)
    {
        float bandStart;
        if (isVerticalLayout)
        {
            bandStart = line.Decoration == TextDecorations.Overline ? line.Start.X : line.Start.X - line.Thickness;
        }
        else
        {
            bandStart = line.Decoration == TextDecorations.Overline ? line.Start.Y - line.Thickness : line.Start.Y;
        }

        return (bandStart, bandStart + line.Thickness);
    }

    /// <summary>
    /// Computes the ink observation band for a decoration line when the glyph's bounding box
    /// can reach it; the outline is not worth observing otherwise.
    /// </summary>
    /// <param name="line">The positioned decoration line, if the decoration is enabled.</param>
    /// <param name="isVerticalLayout">Whether the decoration runs vertically, banding on x instead of y.</param>
    /// <param name="box">The glyph's bounding box, in device pixels.</param>
    /// <param name="bandStart">The band's start on the cross axis, in device pixels.</param>
    /// <param name="bandEnd">The band's end on the cross axis, in device pixels.</param>
    /// <returns><see langword="true"/> when ink can cross the band and it should be observed.</returns>
    private static bool TryGetInkBand(
        in TextDecorationLine? line,
        bool isVerticalLayout,
        in FontRectangle box,
        out float bandStart,
        out float bandEnd)
    {
        if (line is null)
        {
            bandStart = 0F;
            bandEnd = 0F;
            return false;
        }

        (bandStart, bandEnd) = GetDecorationBand(line.Value, isVerticalLayout);
        return isVerticalLayout
            ? box.Right >= bandStart && box.Left <= bandEnd
            : box.Bottom >= bandStart && box.Top <= bandEnd;
    }

    /// <summary>
    /// Emits a decoration line to the renderer as the full, untrimmed line together with the
    /// intervals where the glyph's ink crosses the decoration band. Skip-ink carving is deferred to
    /// the renderer so that a gap can be widened by the drawn thickness and extended across the
    /// boundary into an adjacent glyph, which a per-glyph carve here could not do.
    /// </summary>
    /// <param name="renderer">The glyph renderer that receives the decoration drawing commands.</param>
    /// <param name="line">The positioned decoration line, if the decoration is enabled.</param>
    /// <param name="ink">The ink collector teed into the outline emission, if skip-ink applies to this line.</param>
    private static void EmitDecoration(
        IGlyphRenderer renderer,
        in TextDecorationLine? line,
        GlyphIntersectionCollector? ink)
    {
        if (line is null)
        {
            return;
        }

        TextDecorationLine value = line.Value;
        ReadOnlyMemory<float> intersections = ink is not null ? ink.BuildIntersectionSpan() : default;
        renderer.SetDecoration(value.Decoration, value.Start, value.End, value.Thickness, intersections);
    }

    /// <summary>
    /// Gets a value indicating whether the specified code point should be skipped when
    /// rendering: line terminators participate in layout but have no glyph to draw.
    /// </summary>
    /// <param name="codePoint">The code point.</param>
    /// <returns>The <see cref="bool"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected internal static bool ShouldSkipGlyphRendering(CodePoint codePoint)
        => CodePoint.IsNewLine(codePoint);

    /// <summary>
    /// Returns the size to render/measure the glyph based on the given size and resolution in px units.
    /// </summary>
    /// <param name="pointSize">The font size in pt units.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
    /// <param name="hintingMode">The hinting mode, which may constrain the size to whole pixels.</param>
    /// <returns>The <see cref="float"/>.</returns>
    internal virtual float GetScaledSize(float pointSize, float dpi, HintingMode hintingMode)
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
    /// Attempts to compute the whole pixel advance width for the glyph under full hinting.
    /// Outline formats that grid fit under <see cref="HintingMode.Full"/> override this so
    /// layout can accumulate whole pixel advances matching the fitted outlines. The base
    /// implementation never applies: glyphs without fitted outlines keep their shaped
    /// fractional advances. Implementations must not execute hinting or touch outline
    /// caches, as this runs on the layout hot path.
    /// </summary>
    /// <param name="pointSize">The font size in pt units.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
    /// <param name="hintingMode">The requested hinting mode.</param>
    /// <param name="advancePx">The advance width in whole device pixels.</param>
    /// <returns><see langword="true"/> if a hinted advance applies; otherwise, <see langword="false"/>.</returns>
    public virtual bool TryGetHintedAdvanceWidth(float pointSize, float dpi, HintingMode hintingMode, out float advancePx)
    {
        advancePx = 0F;
        return false;
    }

    /// <summary>
    /// Resolves the hinting mode that will actually shape this glyph's outline. Fonts forced onto
    /// full hinting by the compatibility lists resolve to it under any requested mode.
    /// </summary>
    /// <param name="hintingMode">The requested hinting mode.</param>
    /// <returns>The resolved <see cref="HintingMode"/>.</returns>
    internal virtual HintingMode ResolveHintingMode(HintingMode hintingMode) => hintingMode;

    /// <summary>
    /// Resolves the device-space glyph origin used by both renderer-visible bounds and outline
    /// emission. The complete translation is snapped only after the format confirms that its
    /// cached outline was fitted, so a renderer-owned cache observes the same placement as a
    /// freshly emitted outline.
    /// </summary>
    /// <param name="glyphOrigin">The unsnapped glyph origin in device pixels.</param>
    /// <param name="mode">The glyph layout mode.</param>
    /// <param name="textRun">The text run providing synthetic outline transforms.</param>
    /// <param name="positionOffset">The positioned placement offset in font design units.</param>
    /// <param name="scaledPPEM">The scaled pixels-per-em value used to shape the outline.</param>
    /// <param name="resolvedHintingMode">The hinting mode resolved for the current font.</param>
    /// <returns>The glyph origin used for renderer bounds and outline emission.</returns>
    internal virtual Vector2 ResolveOutlineOrigin(
        Vector2 glyphOrigin,
        GlyphLayoutMode mode,
        TextRun? textRun,
        Vector2 positionOffset,
        float scaledPPEM,
        HintingMode resolvedHintingMode)
    {
        Matrix3x2 outlineTransform = this.GetOutlineTransform(mode, textRun);
        bool axisPreserving = outlineTransform.IsIdentity
            || (mode == GlyphLayoutMode.VerticalRotated && this.GetObliqueSkew(textRun) == 0F);

        if (!axisPreserving || resolvedHintingMode == HintingMode.None)
        {
            return glyphOrigin;
        }

        // Only upright vertical full hinting centres fitted pixel columns on the advance box.
        // Every other placement needs the fitted state and scale but not the curve extent, so
        // formats can avoid inspecting their cached point arrays for those common cases.
        bool includeInkExtent = resolvedHintingMode == HintingMode.Full && mode == GlyphLayoutMode.Vertical;
        if (!this.TryGetFittedOutlinePlacement(scaledPPEM, resolvedHintingMode, includeInkExtent, out FittedOutlinePlacement placement))
        {
            return glyphOrigin;
        }

        Vector2 scaledOffset = (this.Offset + positionOffset) * placement.Scale;
        Vector2 composed = (Vector2.Transform(scaledOffset, outlineTransform) * YInverter) + glyphOrigin;
        Vector2 snapped = SnapComposedTranslation(
            resolvedHintingMode,
            mode,
            composed,
            glyphOrigin.X + (this.AdvanceWidth * placement.Scale.X * 0.5F),
            placement.HasInkExtent,
            placement.MinInkX,
            placement.MaxInkX);

        // Cached fitted points remain untranslated and shareable. Moving only the origin by
        // the snap delta makes metric bounds and emitted geometry reconstruct the same final
        // device translation whether BeginGlyph requests the outline or satisfies a cache hit.
        return glyphOrigin + snapped - composed;
    }

    /// <summary>
    /// Attempts to obtain the fitted outline data required by the shared placement algorithm.
    /// Formats without grid-fitted outlines retain the unsnapped origin through the default
    /// implementation.
    /// </summary>
    /// <param name="scaledPPEM">The scaled pixels-per-em value used to shape the outline.</param>
    /// <param name="resolvedHintingMode">The hinting mode resolved for the current font.</param>
    /// <param name="includeInkExtent">Whether upright vertical centring requires horizontal ink extents.</param>
    /// <param name="placement">The scale and optional fitted horizontal ink extent.</param>
    /// <returns><see langword="true"/> when the outline was fitted; otherwise, <see langword="false"/>.</returns>
    internal virtual bool TryGetFittedOutlinePlacement(
        float scaledPPEM,
        HintingMode resolvedHintingMode,
        bool includeInkExtent,
        out FittedOutlinePlacement placement)
    {
        placement = default;
        return false;
    }

    /// <summary>
    /// Snaps a composed outline translation onto the pixel grid with the per-mode axis policy
    /// every outline format shares. Full hinting snaps both axes, first centring an upright
    /// vertical glyph's fitted ink on its design advance box; standard hinting snaps the
    /// glyph's hinted vertical axis only, which rotated vertical glyphs map onto device X.
    /// Formats resolve their own composed translations and fitted ink extents, then route
    /// them through this single policy so both modes and both formats land identically.
    /// </summary>
    /// <param name="resolvedMode">The resolved hinting mode shaping the outline.</param>
    /// <param name="mode">The glyph layout mode.</param>
    /// <param name="composed">The composed device translation with every term summed.</param>
    /// <param name="boxCentreX">The centre of the design advance box, derived from the glyph origin alone so it stays constant down a vertical column.</param>
    /// <param name="hasFittedInk">Whether fitted ink extents are available for centring.</param>
    /// <param name="fittedMinX">The fitted outline's minimum ink X coordinate.</param>
    /// <param name="fittedMaxX">The fitted outline's maximum ink X coordinate.</param>
    /// <returns>The snapped translation.</returns>
    internal static Vector2 SnapComposedTranslation(HintingMode resolvedMode, GlyphLayoutMode mode, Vector2 composed, float boxCentreX, bool hasFittedInk, float fittedMinX, float fittedMaxX)
    {
        if (resolvedMode == HintingMode.Full)
        {
            float x = composed.X;
            if (mode == GlyphLayoutMode.Vertical && hasFittedInk)
            {
                // Vertical layout centres the design advance box on the column, so the box
                // centre is the column axis itself. It must exclude the per glyph shaped
                // offsets folded into the composed translation: they vary glyph to glyph, and
                // an axis that moves with them scatters equal width glyphs across neighbouring
                // pixels. What the eye centres is the rendered pixel columns, so the ink width
                // is the count of pixel centres the fitted extent covers under centre sample
                // scan conversion, not the rounded geometric extent: the two disagree whenever
                // the extent's fractional phase straddles the samples. An integer translation
                // then lands the first ink column on the floored target, keeping that phase,
                // and flooring the half width difference without a rounding bias puts the odd
                // pixel of an odd width glyph in the extra padding on the right, the classic
                // rasterizer's tie direction.
                float firstColumn = MathF.Ceiling(fittedMinX - 0.5F);
                float inkColumns = MathF.Max(1F, MathF.Ceiling(fittedMaxX - 0.5F) - firstColumn);
                x = MathF.Floor(boxCentreX - (inkColumns * 0.5F)) - firstColumn;
            }

            return new Vector2(MathF.Floor(x + 0.5F), MathF.Floor(composed.Y + 0.5F));
        }

        if (resolvedMode == HintingMode.Standard)
        {
            return mode == GlyphLayoutMode.VerticalRotated
                ? new Vector2(MathF.Floor(composed.X + 0.5F), composed.Y)
                : new Vector2(composed.X, MathF.Floor(composed.Y + 0.5F));
        }

        return composed;
    }

    /// <summary>
    /// Gets the rotation matrix for the glyph based on the layout mode.
    /// </summary>
    /// <param name="mode">The glyph layout mode.</param>
    /// <returns>The rotation matrix.</returns>
    internal static Matrix3x2 GetRotationMatrix(GlyphLayoutMode mode)
    {
        if (mode == GlyphLayoutMode.VerticalRotated)
        {
            // Rotate 90 degrees clockwise.
            return Matrix3x2.CreateRotation(-MathF.PI / 2F);
        }

        return Matrix3x2.Identity;
    }

    /// <summary>
    /// Gets the transform applied to glyph outlines, combining browser-style synthetic oblique
    /// shear with the rotation required by the layout mode.
    /// </summary>
    /// <param name="mode">The glyph layout mode.</param>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <returns>The outline transform.</returns>
    internal Matrix3x2 GetOutlineTransform(GlyphLayoutMode mode, TextRun? textRun)
    {
        // All metrics keep outlines in the glyph's Y-up coordinate system through this transform
        // and convert to device-space Y-down coordinates afterwards. Decorations use
        // GetRotationMatrix directly because browser underlines are not themselves italicized.
        Matrix3x2 transform = CreateObliqueMatrix(this.GetObliqueSkew(textRun));
        transform *= GetRotationMatrix(mode);
        return transform;
    }

    /// <summary>
    /// Describes the format-specific fitted outline data needed to place the outline on the
    /// device grid. The outline scale composes metric offsets in the same coordinate space as
    /// the cached fitted points. Horizontal ink extents are supplied only when upright vertical
    /// full hinting needs to centre the rendered pixel columns on the advance box.
    /// </summary>
    internal readonly struct FittedOutlinePlacement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FittedOutlinePlacement"/> struct.
        /// </summary>
        /// <param name="scale">The number of device pixels per design unit on each axis.</param>
        /// <param name="hasInkExtent">Whether the fitted outline produced a horizontal ink extent.</param>
        /// <param name="minInkX">The fitted outline's minimum horizontal ink coordinate.</param>
        /// <param name="maxInkX">The fitted outline's maximum horizontal ink coordinate.</param>
        public FittedOutlinePlacement(Vector2 scale, bool hasInkExtent, float minInkX, float maxInkX)
        {
            this.Scale = scale;
            this.HasInkExtent = hasInkExtent;
            this.MinInkX = minInkX;
            this.MaxInkX = maxInkX;
        }

        /// <summary>
        /// Gets the number of device pixels per design unit on each axis.
        /// </summary>
        public Vector2 Scale { get; }

        /// <summary>
        /// Gets a value indicating whether the fitted outline produced a horizontal ink extent.
        /// </summary>
        public bool HasInkExtent { get; }

        /// <summary>
        /// Gets the fitted outline's minimum horizontal ink coordinate.
        /// </summary>
        public float MinInkX { get; }

        /// <summary>
        /// Gets the fitted outline's maximum horizontal ink coordinate.
        /// </summary>
        public float MaxInkX { get; }
    }

    /// <summary>
    /// A positioned text decoration line for the current glyph, in device pixels.
    /// </summary>
    private readonly struct TextDecorationLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextDecorationLine"/> struct.
        /// </summary>
        /// <param name="decoration">The decoration the line represents.</param>
        /// <param name="start">The start position of the line.</param>
        /// <param name="end">The end position of the line.</param>
        /// <param name="thickness">The stroke thickness of the line.</param>
        public TextDecorationLine(TextDecorations decoration, Vector2 start, Vector2 end, float thickness)
        {
            this.Decoration = decoration;
            this.Start = start;
            this.End = end;
            this.Thickness = thickness;
        }

        /// <summary>
        /// Gets the decoration the line represents.
        /// </summary>
        public TextDecorations Decoration { get; }

        /// <summary>
        /// Gets the start position of the line.
        /// </summary>
        public Vector2 Start { get; }

        /// <summary>
        /// Gets the end position of the line.
        /// </summary>
        public Vector2 End { get; }

        /// <summary>
        /// Gets the stroke thickness of the line.
        /// </summary>
        public float Thickness { get; }
    }

    /// <summary>
    /// The positioned decoration lines enabled for the current glyph. Absent entries are
    /// disabled or degenerate (zero length or thickness).
    /// </summary>
    private struct TextDecorationGeometry
    {
        /// <summary>
        /// Gets or sets the underline decoration line.
        /// </summary>
        public TextDecorationLine? Underline { get; set; }

        /// <summary>
        /// Gets or sets the strikethrough decoration line.
        /// </summary>
        public TextDecorationLine? Strikeout { get; set; }

        /// <summary>
        /// Gets or sets the overline decoration line.
        /// </summary>
        public TextDecorationLine? Overline { get; set; }
    }

    /// <summary>
    /// Pooled reusable skip-ink instruments: one outline tee and one collector per observable
    /// band. Steady-state decorated rendering rents an instance per glyph from the shared
    /// pool, so no per-glyph allocation occurs; the collectors and tee retain their internal
    /// buffers across uses.
    /// </summary>
    private sealed class SkipInkScratch
    {
        /// <summary>
        /// The shared instance pool. Sized by the default pool capacity; concurrent renders
        /// beyond it fall back to transient instances that are dropped on return.
        /// </summary>
        private static readonly ObjectPool<SkipInkScratch> Pool = new(new PooledObjectPolicy());

        /// <summary>
        /// Gets the collector observing the underline band.
        /// </summary>
        public GlyphIntersectionCollector UnderlineCollector { get; } = new();

        /// <summary>
        /// Gets the collector observing the overline band.
        /// </summary>
        public GlyphIntersectionCollector OverlineCollector { get; } = new();

        /// <summary>
        /// Gets the tee that forwards outline emission while mirroring it into the collectors.
        /// </summary>
        public TeeGlyphRenderer Tee { get; } = new();

        /// <summary>
        /// Rents a scratch instance from the shared pool.
        /// </summary>
        /// <returns>The rented scratch.</returns>
        public static SkipInkScratch Rent() => Pool.Get();

        /// <summary>
        /// Returns the scratch to the shared pool.
        /// </summary>
        public void Release() => Pool.Return(this);

        /// <summary>
        /// Creates scratch instances and clears the tee's forwarding targets on return so
        /// pooled instances do not keep the completed glyph's renderer reachable.
        /// </summary>
        private sealed class PooledObjectPolicy : IPooledObjectPolicy<SkipInkScratch>
        {
            /// <inheritdoc/>
            public SkipInkScratch Create() => new();

            /// <inheritdoc/>
            public bool Return(SkipInkScratch obj)
            {
                obj.Tee.Clear();
                return true;
            }
        }
    }
}
