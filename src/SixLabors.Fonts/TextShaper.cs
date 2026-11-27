// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates logic for shaping text into a positioned glyph stream.
/// </summary>
/// <remarks>
/// <para>
/// The text and its properties are set on a <see cref="TextShapingBuffer"/>, the
/// buffer is shaped against a font, and the glyphs are read back from it. Shaping
/// applies the font's substitution and positioning features but does not wrap the
/// text.
/// </para>
/// <para>
/// <see cref="Shape(Font, TextShapingBuffer)"/> treats the text as unwrapped
/// logical lines separated by hard breaks and resolves mixed-direction text within
/// each line. <see cref="ShapeRun(Font, TextShapingBuffer)"/> shapes one directional
/// run that has already been selected by the caller.
/// </para>
/// <para>
/// Advances and offsets are scaled to the supplied font's size.
/// </para>
/// </remarks>
public static partial class TextShaper
{
    /// <summary>
    /// Shapes the buffer's text as unwrapped logical lines separated by hard
    /// breaks, replacing the buffer's glyphs with each line's visually ordered
    /// glyphs.
    /// </summary>
    /// <param name="font">The font to shape against.</param>
    /// <param name="buffer">The buffer holding the line, which receives the glyphs.</param>
    public static void Shape(Font font, TextShapingBuffer buffer)
    {
        Guard.NotNull(font, nameof(font));
        Guard.NotNull(buffer, nameof(buffer));

        ShapeIntoBuffer(font, buffer, [], TextBidiMode.Normal);
    }

    /// <summary>
    /// Shapes the buffer's text as unwrapped logical lines separated by hard breaks
    /// with the given features turned on, replacing the buffer's glyphs with each
    /// line's visually ordered glyphs.
    /// </summary>
    /// <param name="font">The font to shape against.</param>
    /// <param name="buffer">The buffer holding the line, which receives the glyphs.</param>
    /// <param name="features">The feature tags to turn on for the line.</param>
    public static void Shape(Font font, TextShapingBuffer buffer, Tag[] features)
    {
        Guard.NotNull(font, nameof(font));
        Guard.NotNull(buffer, nameof(buffer));
        Guard.NotNull(features, nameof(features));

        ShapeIntoBuffer(font, buffer, features, TextBidiMode.Normal);
    }

    /// <summary>
    /// Shapes the buffer's text as one directional run, replacing the buffer's
    /// glyphs with the glyphs in reading order.
    /// </summary>
    /// <param name="font">The font to shape against.</param>
    /// <param name="buffer">The buffer holding the run, which receives the glyphs.</param>
    public static void ShapeRun(Font font, TextShapingBuffer buffer)
    {
        Guard.NotNull(font, nameof(font));
        Guard.NotNull(buffer, nameof(buffer));

        ShapeIntoBuffer(font, buffer, [], TextBidiMode.Override);
    }

    /// <summary>
    /// Shapes the buffer's text as one directional run with the given features
    /// turned on, replacing the buffer's glyphs with the glyphs in reading order.
    /// </summary>
    /// <param name="font">The font to shape against.</param>
    /// <param name="buffer">The buffer holding the run, which receives the glyphs.</param>
    /// <param name="features">The feature tags to turn on for the run.</param>
    public static void ShapeRun(Font font, TextShapingBuffer buffer, Tag[] features)
    {
        Guard.NotNull(font, nameof(font));
        Guard.NotNull(buffer, nameof(buffer));
        Guard.NotNull(features, nameof(features));

        ShapeIntoBuffer(font, buffer, features, TextBidiMode.Override);
    }

    /// <summary>
    /// Shapes the buffer under the selected bidirectional contract and publishes
    /// the resulting glyphs.
    /// </summary>
    /// <param name="font">The font to shape against.</param>
    /// <param name="buffer">The buffer holding the text and receiving the glyphs.</param>
    /// <param name="features">The feature tags to turn on.</param>
    /// <param name="bidiMode">Whether the text is a logical line or one directional run.</param>
    private static void ShapeIntoBuffer(Font font, TextShapingBuffer buffer, Tag[] features, TextBidiMode bidiMode)
    {
        if (buffer.Text.IsEmpty)
        {
            _ = buffer.Reserve(0);
            buffer.Commit(0);
            return;
        }

        ShapingScratch scratch = ScratchPool.Get();
        try
        {
            // Public shaping accepts shaping concerns only. Point size enters the
            // projection below; DPI and device coordinates remain renderer inputs.
            TextOptions options = scratch.GetShapingOptions(font, buffer.TextDirection, buffer.Language, buffer.Script, buffer.LayoutMode, buffer.KerningMode, features, bidiMode);

            ShapingBuffer shaped = ShapeCore(buffer.Text, options, scratch, null, true);
            Span<int> lineEnds = default;

            if (bidiMode == TextBidiMode.Normal)
            {
                // ShapeCore deliberately leaves positioned records in logical order
                // because layout cannot choose visual order until line breaking. This
                // API has no soft wrapping, so each newline function fixes a complete
                // line on which the shared L2 transformation can run immediately.
                ReadOnlySpan<int> paragraphEnds = scratch.BidiData.ParagraphEnds;
                lineEnds = buffer.ReserveLineEnds(paragraphEnds.Length);
                int glyphStart = 0;
                for (int paragraph = 0; paragraph <= paragraphEnds.Length; paragraph++)
                {
                    int codePointEnd = paragraph < paragraphEnds.Length ? paragraphEnds[paragraph] : scratch.BidiData.Length;
                    int glyphEnd = glyphStart;
                    while (glyphEnd < shaped.Count && shaped[glyphEnd].CodePointIndex < codePointEnd)
                    {
                        glyphEnd++;
                    }

                    BidiReordering.Reorder(shaped, scratch.BidiRuns, scratch.BidiMap, glyphStart, glyphEnd);
                    if (buffer.LayoutMode.IsVertical())
                    {
                        // HarfBuzz normalizes bottom-to-top runs by reversing whole
                        // graphemes before shaping. UAX #9 has already reversed every
                        // glyph record, so restore the shaped order inside each
                        // right-to-left grapheme without changing its line position.
                        int graphemeStart = glyphStart;
                        while (graphemeStart < glyphEnd)
                        {
                            ref GlyphShapingData first = ref shaped[graphemeStart];
                            int graphemeIndex = first.GraphemeIndex;
                            int graphemeEnd = graphemeStart + 1;
                            while (graphemeEnd < glyphEnd && shaped[graphemeEnd].GraphemeIndex == graphemeIndex)
                            {
                                graphemeEnd++;
                            }

                            if (first.Direction == TextDirection.RightToLeft)
                            {
                                shaped.ReverseRange(graphemeStart, graphemeEnd);
                            }

                            graphemeStart = graphemeEnd;
                        }
                    }

                    if (paragraph < paragraphEnds.Length)
                    {
                        lineEnds[paragraph] = glyphEnd;
                    }

                    glyphStart = glyphEnd;
                }
            }
            else
            {
                // Reuse the layout pipeline's per-run finalization so the public
                // directional-run contract and internal layout cannot diverge.
                FinalizeDirectionalRuns(shaped, buffer.LayoutMode, scratch);
            }

            int count = shaped.Count;
            Span<ShapedGlyph> destination = buffer.Reserve(count);
            for (int i = 0; i < count; i++)
            {
                ref GlyphShapingData shaping = ref shaped[i];
                ref ShapingBuffer.GlyphMetricsEntry entry = ref shaped.MetricsAt(i);
                ref GlyphShapingPosition position = ref shaped.PositionAt(i);
                FontGlyphMetrics glyphMetrics = entry.Metrics;

                // Internal positioning remains in design units. Scale once by the
                // supplied Font.Size before publishing, with no caller-side scaling
                // and no DPI mixed into the shaping result.
                float scale = font.Size / glyphMetrics.UnitsPerEm;
                Vector2 offset = (new Vector2(position.Bounds.X, position.Bounds.Y) + glyphMetrics.Offset) * scale;

                // Mixed vertical layout shapes upright characters vertically but
                // leaves sideways characters horizontal for the layout rotation.
                bool isVertical = AdvancedTypographicUtils.IsVerticalGlyph(shaping.CodePoint, buffer.LayoutMode);

                // Shaping publishes an advance vector in the shaper's Y-up coordinate
                // system. The inactive axis is zero; device-space inversion belongs to
                // the renderer consuming the result.
                float advanceWidth = isVertical ? 0 : entry.GetAdvanceWidth(in position) * scale;
                float advanceHeight = isVertical ? -entry.GetAdvanceHeight(in position) * scale : 0;
                if (isVertical)
                {
                    // Fonts with vertical metrics place the origin above the glyph
                    // ink by the authored top side bearing.
                    float verticalOriginY = glyphMetrics.Bounds.Max.Y + glyphMetrics.TopSideBearing;
                    VerticalMetrics verticalMetrics = glyphMetrics.FontMetrics.VerticalMetrics;
                    if (verticalMetrics.Synthesized)
                    {
                        // HarfBuzz centers glyph extents within the synthesized
                        // ascender-to-descender advance when vertical tables are
                        // absent. Floor preserves its integer midpoint behavior.
                        float fontAdvance = verticalMetrics.Ascender - verticalMetrics.Descender;
                        verticalOriginY = glyphMetrics.Bounds.Max.Y + MathF.Floor((fontAdvance - glyphMetrics.Height) * .5F);
                    }

                    // The vertical origin lies at half the horizontal advance and
                    // at the authored or synthesized Y origin. Subtracting it turns
                    // the horizontal glyph origin into the vertical shaping origin
                    // while preserving the public Y-up coordinate system.
                    offset -= new Vector2(glyphMetrics.AdvanceWidth / 2, verticalOriginY) * scale;
                }

                destination[i] = new ShapedGlyph(glyphMetrics.GlyphId, shaping.StringIndex, shaping.GraphemeIndex, advanceWidth, advanceHeight, offset);
            }

            // Public shaping has no placeholder text runs, so the paragraph loop's
            // shaped-buffer indices are also the published glyph indices.
            buffer.Commit(count);
            buffer.CommitLineEnds(lineEnds.Length);
        }
        finally
        {
            ScratchPool.Return(scratch);
        }
    }
}
