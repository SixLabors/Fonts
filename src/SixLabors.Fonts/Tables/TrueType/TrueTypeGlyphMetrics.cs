// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.TrueType
{
    /// <summary>
    /// Represents a glyph metric from a particular TrueType font face.
    /// </summary>
    public class TrueTypeGlyphMetrics : GlyphMetrics
    {
        private static readonly Vector2 YInverter = new(1, -1);
        private readonly GlyphVector vector;
        private readonly ConcurrentDictionary<float, GlyphVector> scaledVectorCache = new();

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
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
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
                  glyphType,
                  glyphColor)
            => this.vector = vector;

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
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
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
                  glyphType,
                  glyphColor)
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
                this.GlyphType,
                this.GlyphColor);

        /// <summary>
        /// Gets the outline for the current glyph.
        /// </summary>
        /// <returns>The <see cref="GlyphVector"/>.</returns>
        internal GlyphVector GetOutline() => this.vector;

        /// <inheritdoc/>
        internal override void RenderTo(IGlyphRenderer renderer, Vector2 location, Vector2 offset, GlyphLayoutMode mode, TextOptions options)
        {
            // https://www.unicode.org/faq/unsup_char.html
            if (ShouldSkipGlyphRendering(this.CodePoint))
            {
                return;
            }

            float pointSize = this.TextRun.Font?.Size ?? options.Font.Size;
            float dpi = options.Dpi;

            // The glyph vector is rendered offset to the location.
            // For horizontal text, the offset is always zero but vertical or rotated text
            // will be offset against the location.
            location *= dpi;
            offset *= dpi;
            Vector2 renderLocation = location + offset;
            float scaledPPEM = this.GetScaledSize(pointSize, dpi);

            Matrix3x2 rotation = GetRotationMatrix(mode);
            FontRectangle box = this.GetBoundingBox(mode, renderLocation, scaledPPEM);
            GlyphRendererParameters parameters = new(this, this.TextRun, pointSize, dpi, mode);

            if (renderer.BeginGlyph(in box, in parameters))
            {
                if (!ShouldRenderWhiteSpaceOnly(this.CodePoint))
                {
                    if (this.GlyphColor.HasValue && renderer is IColorGlyphRenderer colorSurface)
                    {
                        colorSurface.SetColor(this.GlyphColor.Value);
                    }

                    GlyphVector scaledVector = this.scaledVectorCache.GetOrAdd(scaledPPEM, _ =>
                    {
                        // Create a scaled deep copy of the vector so that we do not alter
                        // the globally cached instance.
                        var clone = GlyphVector.DeepClone(this.vector);
                        Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;

                        var matrix = Matrix3x2.CreateScale(scale);
                        matrix.Translation = this.Offset * scale;
                        GlyphVector.TransformInPlace(ref clone, matrix);

                        float pixelSize = scaledPPEM / 72F;
                        this.FontMetrics.ApplyTrueTypeHinting(options.HintingMode, this, ref clone, scale, pixelSize);

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
                        Vector2 curr = (YInverter * controlPoints[endOfContour].Point) + renderLocation;
                        Vector2 next = (YInverter * controlPoints[startOfContour].Point) + renderLocation;

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
                            next = (YInverter * controlPoints[nextIndex].Point) + renderLocation;

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

                this.RenderDecorationsTo(renderer, location, mode, rotation, scaledPPEM);
            }

            renderer.EndGlyph();
        }
    }
}
