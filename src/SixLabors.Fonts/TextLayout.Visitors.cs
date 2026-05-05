// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <content>
/// Visitor types for streaming laid-out glyphs through the layout pipeline.
/// </content>
internal static partial class TextLayout
{
    /// <summary>
    /// Receives laid-out glyphs streamed from the layout pipeline.
    /// Implementations are value types so the generic dispatch is specialized by the JIT and no boxing or
    /// delegate allocation is required.
    /// </summary>
    internal interface IGlyphLayoutVisitor
    {
        /// <summary>
        /// Invoked before glyphs are streamed for a laid-out line.
        /// </summary>
        /// <param name="lineIndex">The zero-based index of the line in the line-broken text box.</param>
        public void BeginLine(int lineIndex);

        /// <summary>
        /// Invoked once for each laid-out glyph in layout order.
        /// </summary>
        /// <param name="glyph">The laid-out glyph.</param>
        /// <param name="contributesToMeasurement">Whether the glyph contributes to line metrics and measurements.</param>
        public void Visit(in GlyphLayout glyph, bool contributesToMeasurement);

        /// <summary>
        /// Invoked after glyphs have been streamed for a laid-out line.
        /// </summary>
        public void EndLine();
    }
}
