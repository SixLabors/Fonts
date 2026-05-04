// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.TrueType;

/// <summary>
/// Represents a glyph metric from a particular TrueType font face.
/// </summary>
public partial class TrueTypeGlyphMetrics : GlyphMetrics
{
    private static readonly Vector2 YInverter = new(1, -1);
    private readonly GlyphVector vector;
    private readonly ConcurrentDictionary<float, GlyphVector> scaledVectorCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TrueTypeGlyphMetrics"/> class.
    /// </summary>
    /// <param name="font">The font metrics this glyph belongs to.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point for this glyph.</param>
    /// <param name="vector">The glyph outline vector.</param>
    /// <param name="advanceWidth">The advance width in font units.</param>
    /// <param name="advanceHeight">The advance height in font units.</param>
    /// <param name="leftSideBearing">The left side bearing in font units.</param>
    /// <param name="topSideBearing">The top side bearing in font units.</param>
    /// <param name="unitsPerEM">The units per em for the font.</param>
    /// <param name="textAttributes">The text attributes.</param>
    /// <param name="textDecorations">The text decorations.</param>
    /// <param name="glyphType">The glyph type.</param>
    internal TrueTypeGlyphMetrics(
        StreamFontMetrics font,
        ushort glyphId,
        CodePoint codePoint,
        GlyphVector vector,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        GlyphType glyphType)
        : base(
              font,
              glyphId,
              codePoint,
              vector.Bounds,
              advanceWidth,
              advanceHeight,
              leftSideBearing,
              topSideBearing,
              unitsPerEM,
              textAttributes,
              textDecorations,
              glyphType)
        => this.vector = vector;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrueTypeGlyphMetrics"/> class
    /// with explicit offset, scale, and text run for rendering clones.
    /// </summary>
    /// <param name="font">The font metrics this glyph belongs to.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point for this glyph.</param>
    /// <param name="vector">The glyph outline vector.</param>
    /// <param name="advanceWidth">The advance width in font units.</param>
    /// <param name="advanceHeight">The advance height in font units.</param>
    /// <param name="leftSideBearing">The left side bearing in font units.</param>
    /// <param name="topSideBearing">The top side bearing in font units.</param>
    /// <param name="unitsPerEM">The units per em for the font.</param>
    /// <param name="offset">The rendering offset.</param>
    /// <param name="scaleFactor">The scale factor.</param>
    /// <param name="textRun">The text run this glyph is associated with.</param>
    /// <param name="glyphType">The glyph type.</param>
    internal TrueTypeGlyphMetrics(
        StreamFontMetrics font,
        ushort glyphId,
        CodePoint codePoint,
        GlyphVector vector,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        Vector2 offset,
        Vector2 scaleFactor,
        TextRun textRun,
        GlyphType glyphType)
        : base(
              font,
              glyphId,
              codePoint,
              vector.Bounds,
              advanceWidth,
              advanceHeight,
              leftSideBearing,
              topSideBearing,
              unitsPerEM,
              offset,
              scaleFactor,
              textRun,
              glyphType)
        => this.vector = vector;

    /// <inheritdoc/>
    internal override GlyphMetrics CloneForRendering(TextRun textRun)
        => new TrueTypeGlyphMetrics(
            this.FontMetrics,
            this.GlyphId,
            this.CodePoint,
            GlyphVector.DeepClone(this.vector),
            this.AdvanceWidth,
            this.AdvanceHeight,
            this.LeftSideBearing,
            this.TopSideBearing,
            this.UnitsPerEm,
            this.Offset,
            this.ScaleFactor,
            textRun,
            this.GlyphType);

    /// <summary>
    /// Gets the outline for the current glyph.
    /// </summary>
    /// <returns>The <see cref="GlyphVector"/>.</returns>
    internal GlyphVector GetOutline() => this.vector;

    /// <inheritdoc/>
    internal override void RenderTo(
        IGlyphRenderer renderer,
        int graphemeIndex,
        Vector2 glyphOrigin,
        Vector2 decorationOrigin,
        GlyphLayoutMode mode,
        TextOptions options)
    {
        // https://www.unicode.org/faq/unsup_char.html
        if (ShouldSkipGlyphRendering(this.CodePoint))
        {
            return;
        }

        float pointSize = this.TextRun.Font?.Size ?? options.Font.Size;
        float dpi = options.Dpi;

        glyphOrigin *= dpi;
        decorationOrigin *= dpi;
        float scaledPPEM = this.GetScaledSize(pointSize, dpi);

        Matrix3x2 rotation = GetRotationMatrix(mode);
        FontRectangle box = this.GetBoundingBox(mode, glyphOrigin, scaledPPEM);
        GlyphRendererParameters parameters = new(this, this.TextRun, pointSize, dpi, mode, graphemeIndex);

        if (renderer.BeginGlyph(in box, in parameters))
        {
            if (!UnicodeUtility.ShouldRenderWhiteSpaceOnly(this.CodePoint))
            {
                GlyphVector scaledVector = this.scaledVectorCache.GetOrAdd(scaledPPEM, _ =>
                {
                    // Create a scaled deep copy of the vector so that we do not alter
                    // the globally cached instance.
                    GlyphVector clone = GlyphVector.DeepClone(this.vector);
                    Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;

                    Matrix3x2 matrix = Matrix3x2.CreateScale(scale);
                    matrix.Translation = this.Offset * scale;
                    GlyphVector.TransformInPlace(ref clone, matrix);

                    float pixelSize = scaledPPEM / 72F;
                    this.FontMetrics.ApplyTrueTypeHinting(this.GetHintingMode(options.HintingMode), this, ref clone, scale, pixelSize);

                    // Rotation must happen after hinting.
                    GlyphVector.TransformInPlace(ref clone, rotation);
                    return clone;
                });

                IList<ControlPoint> controlPoints = scaledVector.ControlPoints;
                IReadOnlyList<ushort> endPoints = scaledVector.EndPoints;

                int endOfContour = -1;
                for (int i = 0; i < scaledVector.EndPoints.Count; i++)
                {
                    renderer.BeginFigure();
                    int startOfContour = endOfContour + 1;
                    endOfContour = endPoints[i];

                    Vector2 prev;
                    Vector2 curr = (YInverter * controlPoints[endOfContour].Point) + glyphOrigin;
                    Vector2 next = (YInverter * controlPoints[startOfContour].Point) + glyphOrigin;

                    if (controlPoints[endOfContour].OnCurve)
                    {
                        renderer.MoveTo(curr);
                    }
                    else
                    {
                        if (controlPoints[startOfContour].OnCurve)
                        {
                            renderer.MoveTo(next);
                        }
                        else
                        {
                            // If both first and last points are off-curve, start at their middle.
                            Vector2 startPoint = (curr + next) * .5F;
                            renderer.MoveTo(startPoint);
                        }
                    }

                    int length = endOfContour - startOfContour + 1;
                    for (int p = 0; p < length; p++)
                    {
                        prev = curr;
                        curr = next;
                        int currentIndex = startOfContour + p;
                        int nextIndex = startOfContour + ((p + 1) % length);
                        int prevIndex = startOfContour + ((length + p - 1) % length);
                        next = (YInverter * controlPoints[nextIndex].Point) + glyphOrigin;

                        if (controlPoints[currentIndex].OnCurve)
                        {
                            // This is a straight line.
                            renderer.LineTo(curr);
                        }
                        else
                        {
                            Vector2 prev2 = prev;
                            Vector2 next2 = next;

                            if (!controlPoints[prevIndex].OnCurve)
                            {
                                prev2 = (curr + prev) * .5F;
                                renderer.LineTo(prev2);
                            }

                            if (!controlPoints[nextIndex].OnCurve)
                            {
                                next2 = (curr + next) * .5F;
                            }

                            renderer.LineTo(prev2);
                            renderer.QuadraticBezierTo(curr, next2);
                        }
                    }

                    renderer.EndFigure();
                }
            }

            renderer.EndGlyph();
            this.RenderDecorationsTo(renderer, decorationOrigin, mode, rotation, scaledPPEM, options);
        }
    }
}
