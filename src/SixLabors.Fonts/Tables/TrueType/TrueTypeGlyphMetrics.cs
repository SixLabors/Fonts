// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.TrueType
{
    /// <summary>
    /// Represents a glyph metric from a particular TrueType font face.
    /// </summary>
    public class TrueTypeGlyphMetrics : GlyphMetrics
    {
        private static readonly Vector2 MirrorScale = new(1, -1);
        private readonly GlyphVector vector;
        private readonly Dictionary<float, GlyphVector> scaledVector = new();
        private TextRun? textRun;

        internal TrueTypeGlyphMetrics(
            StreamFontMetrics font,
            CodePoint codePoint,
            GlyphVector vector,
            ushort advanceWidth,
            ushort advanceHeight,
            short leftSideBearing,
            short topSideBearing,
            ushort unitsPerEM,
            ushort glyphId,
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
            : base(font, codePoint, vector.GetBounds(), advanceWidth, advanceHeight, leftSideBearing, topSideBearing, unitsPerEM, glyphId, glyphType, glyphColor)
            => this.vector = vector;

        /// <inheritdoc/>
        internal override GlyphMetrics CloneForRendering(TextRun textRun, CodePoint codePoint)
        {
            StreamFontMetrics fontMetrics = this.FontMetrics;
            Vector2 offset = this.Offset;
            Vector2 scaleFactor = this.ScaleFactor;
            if (textRun.TextAttributes.HasFlag(TextAttributes.Subscript))
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SubscriptXSize / units, fontMetrics.SubscriptYSize / units);
                offset = new(this.FontMetrics.SubscriptXOffset, this.FontMetrics.SubscriptYOffset);
            }
            else if (textRun.TextAttributes.HasFlag(TextAttributes.Superscript))
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SuperscriptXSize / units, fontMetrics.SuperscriptYSize / units);
                offset = new(fontMetrics.SuperscriptXOffset, -fontMetrics.SuperscriptYOffset);
            }

            return new TrueTypeGlyphMetrics(
                fontMetrics,
                codePoint,
                GlyphVector.DeepClone(this.vector),
                this.AdvanceWidth,
                this.AdvanceHeight,
                this.LeftSideBearing,
                this.TopSideBearing,
                this.UnitsPerEm,
                this.GlyphId,
                this.GlyphType,
                this.GlyphColor)
            {
                Offset = offset,
                ScaleFactor = scaleFactor,
                textRun = textRun
            };
        }

        /// <summary>
        /// Gets the outline for the current glyph.
        /// </summary>
        /// <returns>The <see cref="GlyphOutline"/>.</returns>
        public GlyphOutline GetOutline() => this.vector.GetOutline();

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="location">The location.</param>
        /// <param name="options">The options used to influence the rendering of this glyph.</param>
        /// <exception cref="NotSupportedException">Too many control points</exception>
        internal override void RenderTo(IGlyphRenderer surface, float pointSize, Vector2 location, TextOptions options)
        {
            // https://www.unicode.org/faq/unsup_char.html
            if (ShouldSkipGlyphRendering(this.CodePoint))
            {
                return;
            }

            // TODO: Move to base class
            float dpi = options.Dpi;
            location *= dpi;
            float scaledPPEM = dpi * pointSize;
            bool forcePPEMToInt = (this.FontMetrics.HeadFlags & HeadTable.HeadFlags.ForcePPEMToInt) != 0;

            if (forcePPEMToInt)
            {
                scaledPPEM = MathF.Round(scaledPPEM);
            }

            FontRectangle box = this.GetBoundingBox(location, scaledPPEM);

            // TextRun is never null here as rendering is only accessable via a Glyph which
            // uses the cloned metrics instance.
            var parameters = new GlyphRendererParameters(this, this.textRun!, pointSize, dpi);

            if (surface.BeginGlyph(box, parameters))
            {
                if (!ShouldRenderWhiteSpaceOnly(this.CodePoint))
                {
                    if (this.GlyphColor.HasValue && surface is IColorGlyphRenderer colorSurface)
                    {
                        colorSurface.SetColor(this.GlyphColor.Value);
                    }

                    if (!this.scaledVector.TryGetValue(scaledPPEM, out GlyphVector scaledVector))
                    {
                        // Scale and translate the glyph
                        Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
                        var transform = Matrix3x2.CreateScale(scale);
                        transform.Translation = this.Offset * scale * MirrorScale;
                        scaledVector = GlyphVector.Transform(this.vector, transform);
                        this.FontMetrics.ApplyTrueTypeHinting(options.HintingMode, this, ref scaledVector, scale, scaledPPEM);
                        this.scaledVector[scaledPPEM] = scaledVector;
                    }

                    GlyphOutline outline = scaledVector.GetOutline();
                    ReadOnlySpan<Vector2> controlPoints = outline.ControlPoints.Span;
                    ReadOnlySpan<ushort> endPoints = outline.EndPoints.Span;
                    ReadOnlySpan<bool> onCurves = outline.OnCurves.Span;

                    int endOfContour = -1;
                    for (int i = 0; i < outline.EndPoints.Length; i++)
                    {
                        surface.BeginFigure();
                        int startOfContour = endOfContour + 1;
                        endOfContour = endPoints[i];

                        Vector2 prev;
                        Vector2 curr = (MirrorScale * controlPoints[endOfContour]) + location;
                        Vector2 next = (MirrorScale * controlPoints[startOfContour]) + location;

                        if (onCurves[endOfContour])
                        {
                            surface.MoveTo(curr);
                        }
                        else
                        {
                            if (onCurves[startOfContour])
                            {
                                surface.MoveTo(next);
                            }
                            else
                            {
                                // If both first and last points are off-curve, start at their middle.
                                Vector2 startPoint = (curr + next) * .5F;
                                surface.MoveTo(startPoint);
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
                            next = (MirrorScale * controlPoints[nextIndex]) + location;

                            if (onCurves[currentIndex])
                            {
                                // This is a straight line.
                                surface.LineTo(curr);
                            }
                            else
                            {
                                Vector2 prev2 = prev;
                                Vector2 next2 = next;

                                if (!onCurves[prevIndex])
                                {
                                    prev2 = (curr + prev) * .5F;
                                    surface.LineTo(prev2);
                                }

                                if (!onCurves[nextIndex])
                                {
                                    next2 = (curr + next) * .5F;
                                }

                                surface.LineTo(prev2);
                                surface.QuadraticBezierTo(curr, next2);
                            }
                        }

                        surface.EndFigure();
                    }
                }

                // TODO: Move to base class
                (Vector2 Start, Vector2 End, float Thickness) GetEnds(float thickness, float position)
                {
                    Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor * MirrorScale;
                    Vector2 offset = location + (this.Offset * scale * MirrorScale);

                    // Calculate the correct advance for the line.
                    float width = this.AdvanceWidth;
                    if (width == 0)
                    {
                        // For zero advance glyphs we must calculate our advance width from bearing + width;
                        width = this.LeftSideBearing + this.Width;
                    }

                    Vector2 tl = (new Vector2(0, position) * scale) + offset;
                    Vector2 tr = (new Vector2(width, position) * scale) + offset;
                    Vector2 bl = (new Vector2(0, position + thickness) * scale) + offset;

                    return (tl, tr, tl.Y - bl.Y);
                }

                // TODO: Move to base class
                void DrawLine(float thickness, float position)
                {
                    surface.BeginFigure();

                    (Vector2 start, Vector2 end, float finalThickness) = GetEnds(thickness, position);
                    var halfHeight = new Vector2(0, -finalThickness * .5F);

                    Vector2 tl = start - halfHeight;
                    Vector2 tr = end - halfHeight;
                    Vector2 bl = start + halfHeight;
                    Vector2 br = end + halfHeight;

                    // Clamp the horizontal components to a whole pixel.
                    tl.Y = MathF.Ceiling(tl.Y);
                    tr.Y = MathF.Ceiling(tr.Y);
                    br.Y = MathF.Floor(br.Y);
                    bl.Y = MathF.Floor(bl.Y);

                    // Do the same for vertical components.
                    tl.X = MathF.Floor(tl.X);
                    tr.X = MathF.Floor(tr.X);
                    br.X = MathF.Floor(br.X);
                    bl.X = MathF.Floor(bl.X);

                    surface.MoveTo(tl);
                    surface.LineTo(bl);
                    surface.LineTo(br);
                    surface.LineTo(tr);

                    surface.EndFigure();
                }

                // TODO: Move to base class
                void SetDecoration(TextDecorations decorationType, float thickness, float position)
                {
                    (Vector2 start, Vector2 end, float calcThickness) = GetEnds(thickness, position);
                    ((IGlyphDecorationRenderer)surface).SetDecoration(decorationType, start, end, calcThickness);
                }

                // There's no built in metrics for these values so we will need to infer them from the other metrics.
                // Offset to avoid clipping.
                float overlineThickness = this.FontMetrics.UnderlineThickness;
                float overlinePosition = this.FontMetrics.Ascender - (overlineThickness * .5F);
                if (surface is IGlyphDecorationRenderer decorationSurface)
                {
                    // allow the rendered to override the decorations to attach
                    TextDecorations decorations = decorationSurface.EnabledDecorations();
                    if ((decorations & TextDecorations.Underline) == TextDecorations.Underline)
                    {
                        SetDecoration(TextDecorations.Underline, this.FontMetrics.UnderlineThickness, this.FontMetrics.UnderlinePosition);
                    }

                    if ((decorations & TextDecorations.Strikeout) == TextDecorations.Strikeout)
                    {
                        SetDecoration(TextDecorations.Strikeout, this.FontMetrics.StrikeoutSize, this.FontMetrics.StrikeoutPosition);
                    }

                    if ((decorations & TextDecorations.Overline) == TextDecorations.Overline)
                    {
                        SetDecoration(TextDecorations.Overline, overlineThickness, overlinePosition);
                    }
                }
                else
                {
                    // TextRun is never null here as rendering is only accessable via a Glyph which
                    // uses the cloned metrics instance.
                    if ((this.textRun!.TextDecorations & TextDecorations.Underline) == TextDecorations.Underline)
                    {
                        DrawLine(this.FontMetrics.UnderlineThickness, this.FontMetrics.UnderlinePosition);
                    }

                    if ((this.textRun!.TextDecorations & TextDecorations.Strikeout) == TextDecorations.Strikeout)
                    {
                        DrawLine(this.FontMetrics.StrikeoutSize, this.FontMetrics.StrikeoutPosition);
                    }

                    if ((this.textRun!.TextDecorations & TextDecorations.Overline) == TextDecorations.Overline)
                    {
                        DrawLine(overlineThickness, overlinePosition);
                    }
                }
            }

            surface.EndGlyph();
        }
    }
}
