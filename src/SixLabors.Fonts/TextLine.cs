// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// A shaped line of text — an ordered sequence of <see cref="GlyphLayoutData"/> entries plus
/// per-line aggregate metrics (advance, ascender, descender, etc.) used to position the line
/// during layout.
/// </summary>
internal sealed class TextLine
{
    private readonly List<GlyphLayoutData> data;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextLine"/> class with a small default capacity.
    /// </summary>
    public TextLine() => this.data = new List<GlyphLayoutData>(16);

    /// <summary>
    /// Initializes a new instance of the <see cref="TextLine"/> class with the specified initial
    /// entry capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity for the internal entry list.</param>
    public TextLine(int capacity) => this.data = new List<GlyphLayoutData>(capacity);

    /// <summary>
    /// Initializes a new instance of the <see cref="TextLine"/> class by copying another line.
    /// </summary>
    /// <param name="source">The line to copy.</param>
    public TextLine(TextLine source)
    {
        this.data = [.. source.data];
        this.SkipJustification = source.SkipJustification;
        this.ScaledLineAdvance = source.ScaledLineAdvance;
        this.ScaledMaxLineHeight = source.ScaledMaxLineHeight;
        this.ScaledMaxAscender = source.ScaledMaxAscender;
        this.ScaledMaxDescender = source.ScaledMaxDescender;
        this.ScaledMaxDelta = source.ScaledMaxDelta;
        this.ScaledMinY = source.ScaledMinY;
    }

    /// <summary>
    /// Gets the number of <see cref="GlyphLayoutData"/> entries in this line.
    /// </summary>
    public int Count => this.data.Count;

    /// <summary>
    /// Gets the number of graphemes in this line.
    /// </summary>
    public int GraphemeCount
    {
        get
        {
            int count = 0;
            int lastGraphemeIndex = -1;
            for (int i = 0; i < this.data.Count; i++)
            {
                int graphemeIndex = this.data[i].GraphemeIndex;
                if (graphemeIndex == lastGraphemeIndex)
                {
                    continue;
                }

                count++;
                lastGraphemeIndex = graphemeIndex;
            }

            return count;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this line should be skipped during text justification.
    /// Set by <see cref="Finalize"/> for lines that end a paragraph.
    /// </summary>
    public bool SkipJustification { get; private set; }

    /// <summary>
    /// Gets the scaled advance contributed to line layout and measurement.
    /// </summary>
    public float ScaledLineAdvance { get; private set; }

    /// <summary>
    /// Gets the greatest scaled line height across all entries, multiplied by the configured
    /// line-spacing factor.
    /// </summary>
    public float ScaledMaxLineHeight { get; private set; } = -1;

    /// <summary>
    /// Gets the greatest scaled ascender across all entries in this line.
    /// </summary>
    public float ScaledMaxAscender { get; private set; } = -1;

    /// <summary>
    /// Gets the greatest scaled descender across all entries in this line.
    /// </summary>
    public float ScaledMaxDescender { get; private set; } = -1;

    /// <summary>
    /// Gets the greatest scaled symmetric-metrics delta across all entries in this line.
    /// Browsers adjust ascender/descender symmetrically for baseline alignment; this captures
    /// that adjustment.
    /// </summary>
    public float ScaledMaxDelta { get; private set; } = float.MinValue;

    /// <summary>
    /// Gets the smallest (most negative) scaled Y position across all entries in this line.
    /// Used to detect ink that extends above the typographic ascender (for example stacked
    /// marks in Tibetan) so the layout engine can reserve extra ascent.
    /// </summary>
    public float ScaledMinY { get; private set; }

    /// <summary>
    /// Gets the <see cref="GlyphLayoutData"/> entry at the given index.
    /// </summary>
    /// <param name="index">The zero-based index into this line.</param>
    /// <returns>The entry at the given index.</returns>
    public GlyphLayoutData this[int index] => this.data[index];

    /// <summary>
    /// Counts the glyph entries emitted from this line.
    /// </summary>
    /// <returns>The number of glyph entries that layout will emit for this line.</returns>
    public int CountGlyphLayouts()
    {
        int count = 0;
        for (int i = 0; i < this.data.Count; i++)
        {
            count += this.data[i].Metrics.Length;
        }

        return count;
    }

    /// <summary>
    /// Appends a shaped entry to this line, updating the aggregated line-level metrics.
    /// </summary>
    /// <param name="metrics">The entry's contiguous slice of the line's shaped glyph storage.</param>
    /// <param name="font">The font used to shape and render this entry.</param>
    /// <param name="pointSize">The point size at which the entry is rendered.</param>
    /// <param name="scaledAdvance">The scaled advance contributed by this entry.</param>
    /// <param name="scaledLineHeight">The scaled line height contributed by this entry (before line-spacing).</param>
    /// <param name="scaledAscender">The scaled typographic ascender.</param>
    /// <param name="scaledDescender">The scaled typographic descender.</param>
    /// <param name="scaledDelta">The symmetric metrics delta applied during line-box construction.</param>
    /// <param name="bidiRun">The bidi run this entry belongs to.</param>
    /// <param name="graphemeIndex">The grapheme index in the source text.</param>
    /// <param name="isLastInGrapheme">Whether this entry is the last codepoint in its grapheme cluster.</param>
    /// <param name="codePointIndex">The codepoint index in the source text.</param>
    /// <param name="graphemeCodePointIndex">The index of the codepoint within its grapheme cluster.</param>
    /// <param name="isTransformed">Whether the entry participates in a transformed (rotated) vertical layout.</param>
    /// <param name="isDecomposed">Whether the entry was produced by Unicode decomposition.</param>
    /// <param name="stringIndex">The character index in the source string.</param>
    /// <param name="layoutMode">The glyph-level layout mode to use for ink bounds computation.</param>
    /// <param name="lineSpacing">The line-spacing factor to apply to <paramref name="scaledLineHeight"/>.</param>
    /// <param name="hyphenationMarkerIndex">The marker index to use if this entry becomes a selected soft-hyphen break.</param>
    public void Add(
        ReadOnlyMemory<PositionedGlyphMetrics> metrics,
        Font font,
        float pointSize,
        float scaledAdvance,
        float scaledLineHeight,
        float scaledAscender,
        float scaledDescender,
        float scaledDelta,
        BidiRun bidiRun,
        int graphemeIndex,
        bool isLastInGrapheme,
        int codePointIndex,
        int graphemeCodePointIndex,
        bool isTransformed,
        bool isDecomposed,
        int stringIndex,
        GlyphLayoutMode layoutMode,
        float lineSpacing,
        int hyphenationMarkerIndex = GlyphLayoutData.NoHyphenationMarker)
    {
        // Apply LineSpacing to scaledLineHeight before storing
        scaledLineHeight *= lineSpacing;

        // Reset metrics.
        // We track the maximum metrics for each line to ensure glyphs can be aligned.
        // Layout consumes every positioned glyph advance. Grapheme boundaries govern
        // text semantics, but do not collapse multiple positioned glyph advances.
        this.ScaledLineAdvance += scaledAdvance;

        this.ScaledMaxLineHeight = MathF.Max(this.ScaledMaxLineHeight, scaledLineHeight);
        this.ScaledMaxAscender = MathF.Max(this.ScaledMaxAscender, scaledAscender);
        this.ScaledMaxDescender = MathF.Max(this.ScaledMaxDescender, scaledDescender);
        this.ScaledMaxDelta = MathF.Max(this.ScaledMaxDelta, scaledDelta);

        // Track the true top of the ink in device space (Y down, baseline at 0).
        // For scripts with stacked marks (Tibetan, etc) this can be significantly
        // above the typographic ascender, so we cannot trust ascender alone.
        float scaledMinY = 0;
        ReadOnlySpan<PositionedGlyphMetrics> metricsSpan = metrics.Span;
        for (int i = 0; i < metricsSpan.Length; i++)
        {
            FontGlyphMetrics metric = metricsSpan[i].Metrics;
            if (FontGlyphMetrics.ShouldSkipGlyphRendering(metric.CodePoint))
            {
                continue;
            }

            FontRectangle bbox = metric.GetBoundingBox(layoutMode, Vector2.Zero, pointSize, metricsSpan[i].TextRun, metricsSpan[i].Offset, new Vector2(metricsSpan[i].AdvanceWidth, metricsSpan[i].AdvanceHeight));
            scaledMinY = MathF.Min(scaledMinY, bbox.Y);
        }

        // ScaledMinY is the minimum ink Y over all glyphs in this line, in Y down.
        // It is usually <= 0; more negative means more ink above the baseline.
        if (this.data.Count == 0)
        {
            this.ScaledMinY = scaledMinY;
        }
        else
        {
            this.ScaledMinY = MathF.Min(this.ScaledMinY, scaledMinY);
        }

        this.data.Add(new GlyphLayoutData(
            metrics,
            font,
            pointSize,
            scaledAdvance,
            scaledLineHeight,
            scaledAscender,
            scaledDescender,
            scaledDelta,
            scaledMinY,
            bidiRun,
            graphemeIndex,
            isLastInGrapheme,
            codePointIndex,
            graphemeCodePointIndex,
            isTransformed,
            isDecomposed,
            stringIndex,
            hyphenationMarkerIndex));
    }

    /// <summary>
    /// Adds an inline placeholder entry at an existing source codepoint position without consuming source text.
    /// </summary>
    /// <param name="placeholderGlyph">The placeholder glyph metrics.</param>
    /// <param name="run">The shaped run carrying the placeholder's font, point size, text run, and bidi run.</param>
    /// <param name="offset">The source codepoint index at the placeholder insertion point.</param>
    /// <param name="graphemeIndex">The source grapheme index at the placeholder insertion point.</param>
    /// <param name="stringIndex">The source UTF-16 index at the placeholder insertion point.</param>
    /// <param name="isHorizontalLayout"><see langword="true"/> when the current layout advances horizontally.</param>
    /// <param name="isVerticalMixedLayout"><see langword="true"/> when the current layout is vertical mixed.</param>
    /// <param name="lineSpacing">The line-spacing factor to apply to placeholder line height.</param>
    public void AddPlaceholder(
        FontGlyphMetrics placeholderGlyph,
        in ShapedTextRun run,
        int offset,
        int graphemeIndex,
        int stringIndex,
        bool isHorizontalLayout,
        bool isVerticalMixedLayout,
        float lineSpacing)
    {
        bool isPlaceholderHorizontal = isHorizontalLayout || isVerticalMixedLayout;
        float placeholderAdvance = isPlaceholderHorizontal
            ? placeholderGlyph.AdvanceWidth
            : placeholderGlyph.AdvanceHeight;

        Vector2 placeholderScale = new(
            run.PointSize / placeholderGlyph.ScaleFactor.X,
            run.PointSize / placeholderGlyph.ScaleFactor.Y);

        placeholderAdvance *= isPlaceholderHorizontal ? placeholderScale.X : placeholderScale.Y;

        GlyphLayoutMode placeholderMode = isHorizontalLayout
            ? GlyphLayoutMode.Horizontal
            : GlyphLayoutMode.Vertical;

        FontRectangle placeholderBox = placeholderGlyph.GetBoundingBox(placeholderMode, Vector2.Zero, run.PointSize, run.TextRun, Vector2.Zero, new Vector2(placeholderGlyph.AdvanceWidth, placeholderGlyph.AdvanceHeight));

        IMetricsHeader metricsHeader = isPlaceholderHorizontal
            ? placeholderGlyph.FontMetrics.HorizontalMetrics
            : placeholderGlyph.FontMetrics.VerticalMetrics;

        // Placeholder bounds can extend beyond the surrounding run font's
        // normal ascender/descender band. Keep the run font line-box model as
        // the baseline contribution, then expand only the side the placeholder
        // actually overhangs so following lines reserve enough space.
        float placeholderScaleY = run.PointSize / placeholderGlyph.ScaleFactor.Y;
        float placeholderLineHeight = placeholderGlyph.UnitsPerEm * placeholderScaleY;
        float placeholderDelta = ((metricsHeader.LineHeight * placeholderScaleY) - placeholderLineHeight) * .5F;
        float placeholderAscender = (metricsHeader.Ascender * placeholderScaleY) - placeholderDelta;
        float placeholderDescender = Math.Abs(metricsHeader.Descender * placeholderScaleY) - placeholderDelta;
        placeholderAscender = MathF.Max(placeholderAscender, -placeholderBox.Top);
        placeholderDescender = MathF.Max(placeholderDescender, placeholderBox.Bottom);
        placeholderLineHeight = MathF.Max(
            placeholderLineHeight,
            placeholderAscender + placeholderDescender + (2 * placeholderDelta));

        // Placeholders share the source codepoint offset at their insertion point,
        // but they do not consume source grapheme, codepoint, or UTF-16 indexes.
        // Generated glyphs are not part of the shaped stream, so the entry owns
        // its single-glyph storage.
        this.Add(
            new PositionedGlyphMetrics[] { new(placeholderGlyph, placeholderGlyph.AdvanceWidth, placeholderGlyph.AdvanceHeight, Vector2.Zero, run.TextRun) },
            run.Font,
            run.PointSize,
            placeholderAdvance,
            placeholderLineHeight,
            placeholderAscender,
            placeholderDescender,
            placeholderDelta,
            run.BidiRun,
            graphemeIndex,
            true,
            offset,
            0,
            false,
            false,
            stringIndex,
            placeholderMode,
            lineSpacing);
    }

    /// <summary>
    /// Inserts all entries from <paramref name="textLine"/> into this line at the given index
    /// and recomputes aggregated metrics.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="textLine">The line whose entries should be inserted.</param>
    public void InsertAt(int index, TextLine textLine)
    {
        this.data.InsertRange(index, textLine.data);
        RecalculateLineMetrics(this);
    }

    /// <summary>
    /// Gets the marker advance for a selected soft-hyphen entry.
    /// </summary>
    /// <param name="index">The soft-hyphen entry index in this line.</param>
    /// <param name="hyphenationMarkers">The markers prepared with the logical line.</param>
    /// <returns>The scaled advance of the visible hyphenation marker.</returns>
    public float GetHyphenationMarkerAdvance(
        int index,
        List<GlyphLayoutData> hyphenationMarkers)
        => hyphenationMarkers[this.data[index].HyphenationMarkerIndex].ScaledAdvance;

    /// <summary>
    /// Replaces a selected soft-hyphen entry with its prepared visible marker.
    /// </summary>
    /// <param name="index">The soft-hyphen entry index in this line.</param>
    /// <param name="hyphenationMarkers">The markers prepared with the logical line.</param>
    public void ApplyHyphenationMarker(
        int index,
        List<GlyphLayoutData> hyphenationMarkers)
    {
        this.data[index] = hyphenationMarkers[this.data[index].HyphenationMarkerIndex];
        RecalculateLineMetrics(this);
    }

    /// <summary>
    /// Applies an ellipsis marker to the end of this line.
    /// </summary>
    /// <param name="markerCodePoint">The marker codepoint to append.</param>
    /// <param name="scaledWrappingLength">The wrapping length in inches.</param>
    /// <param name="options">The text options used for layout.</param>
    public void ApplyEllipsisMarker(
        CodePoint markerCodePoint,
        float scaledWrappingLength,
        TextOptions options)
    {
        // The marker replaces the hidden tail, so breakable whitespace at the
        // truncation edge is removed before we choose the marker style or decide
        // how many graphemes fit.
        this.RemoveTrailingBreakingWhitespace();

        GlyphLayoutData anchor = this.data[^1];
        GlyphLayoutData marker = TextLayout.CreateGeneratedMarker(
            anchor.Metrics.Span[0],
            anchor.PointSize,
            anchor.BidiRun,
            anchor.GraphemeIndex,
            anchor.IsLastInGrapheme,
            anchor.CodePointIndex,
            anchor.GraphemeCodePointIndex,
            anchor.StringIndex,
            markerCodePoint,
            options.LayoutMode,
            anchor.Font,
            options);

        while (this.data.Count > 0 &&
            this.ScaledLineAdvance + marker.ScaledAdvance > scaledWrappingLength)
        {
            // Remove a whole grapheme at a time. Truncating through a decomposed
            // cluster would corrupt the same source unit that selection and caret
            // metrics expose as indivisible.
            this.RemoveLastGrapheme();
        }

        // CSS block ellipsis allows the marker to displace the whole final line.
        // That means an overflowing line can become marker-only, but only because
        // hidden text exists after the clamp point.
        this.data.Add(marker);
        RecalculateLineMetrics(this);
    }

    /// <summary>
    /// Removes trailing breakable whitespace from the line.
    /// </summary>
    /// <param name="preserveTrailingBreakingWhitespace">
    /// When <see langword="true"/>, keeps ordinary trailing breaking whitespace for editor interaction.
    /// </param>
    private void RemoveTrailingBreakingWhitespace(bool preserveTrailingBreakingWhitespace = false)
    {
        int index = this.data.Count;
        while (index > 1)
        {
            CodePoint point = this.data[index - 1].CodePoint;
            if (!CodePoint.IsWhiteSpace(point) || CodePoint.IsNonBreakingSpace(point))
            {
                break;
            }

            if (preserveTrailingBreakingWhitespace && !CodePoint.IsNewLine(point))
            {
                break;
            }

            index--;
        }

        if (index < this.data.Count)
        {
            this.data.RemoveRange(index, this.data.Count - index);
            RecalculateLineMetrics(this);
        }
    }

    /// <summary>
    /// Removes the last complete grapheme from the line.
    /// </summary>
    private void RemoveLastGrapheme()
    {
        int end = this.data.Count - 1;
        int graphemeIndex = this.data[end].GraphemeIndex;
        int start = end;
        while (start > 0 && this.data[start - 1].GraphemeIndex == graphemeIndex)
        {
            start--;
        }

        this.data.RemoveRange(start, end - start + 1);
        RecalculateLineMetrics(this);
    }

    /// <summary>
    /// Splits this line at the first non-whitespace glyph whose cumulative advance meets or
    /// exceeds <paramref name="length"/>. On success, the split-off tail is returned as a new
    /// line and removed from this one; both lines have their aggregated metrics recomputed.
    /// </summary>
    /// <param name="length">The scaled advance threshold at which to split.</param>
    /// <param name="result">The trailing portion of the split, or <see langword="null"/> if no split was performed.</param>
    /// <returns><see langword="true"/> if a split occurred; otherwise <see langword="false"/>.</returns>
    public bool TrySplitAt(float length, [NotNullWhen(true)] out TextLine? result)
    {
        float advance = this.data[0].ScaledAdvance;

        // Ensure at least one glyph is in the line.
        // trailing whitespace should be ignored as it is trimmed
        // on finalization.
        for (int i = 1; i < this.data.Count; i++)
        {
            GlyphLayoutData glyph = this.data[i];
            advance += glyph.ScaledAdvance;
            if (CodePoint.IsWhiteSpace(glyph.CodePoint))
            {
                continue;
            }

            if (advance >= length)
            {
                int count = this.data.Count - i;
                result = new TextLine(count);
                result.data.AddRange(this.data.GetRange(i, count));
                RecalculateLineMetrics(result);

                this.data.RemoveRange(i, count);
                RecalculateLineMetrics(this);
                return true;
            }
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Splits this line at the glyph immediately preceding the supplied <see cref="LineBreak"/>
    /// wrap position. When <paramref name="keepAll"/> is set, the split is delayed until
    /// the nearest boundary outside a CSS keep-all word unit sequence.
    /// </summary>
    /// <param name="lineBreak">The resolved line-break opportunity.</param>
    /// <param name="keepAll">When <see langword="true"/>, avoid breaking within keep-all word unit sequences.</param>
    /// <param name="result">The trailing portion of the split, or <see langword="null"/> if no split was performed.</param>
    /// <returns><see langword="true"/> if a split occurred; otherwise <see langword="false"/>.</returns>
    public bool TrySplitAt(LineBreak lineBreak, bool keepAll, [NotNullWhen(true)] out TextLine? result)
    {
        int index = this.data.Count;
        while (index > 0)
        {
            if (this.data[--index].CodePointIndex == lineBreak.PositionWrap)
            {
                // One source codepoint can emit several layout entries. The
                // reverse search lands on its final entry, so rewind to the first
                // entry or the preceding line would retain part of the grapheme
                // after its hard break.
                while (index > 0 && this.data[index - 1].CodePointIndex == lineBreak.PositionWrap)
                {
                    index--;
                }

                break;
            }
        }

        // CSS word-break: keep-all suppresses implicit breaks between typographic letter units.
        if (index > 0
            && !lineBreak.Required
            && keepAll
            && this.IsKeepAllSuppressedBreak(index))
        {
            while (index > 0 && this.IsKeepAllSuppressedBreak(index))
            {
                index--;
            }
        }

        if (index == 0)
        {
            result = null;
            return false;
        }

        // Create a new line ensuring we capture the initial metrics.
        int count = this.data.Count - index;
        result = new TextLine(count);
        result.data.AddRange(this.data.GetRange(index, count));
        RecalculateLineMetrics(result);

        // Remove those items from this line.
        this.data.RemoveRange(index, count);
        RecalculateLineMetrics(this);

        return true;
    }

    /// <summary>
    /// Splits a terminal hard-break grapheme into its own line.
    /// </summary>
    /// <param name="result">The terminal hard-break line, or <see langword="null"/> if no split was performed.</param>
    /// <returns><see langword="true"/> if a terminal hard break was split; otherwise <see langword="false"/>.</returns>
    public bool TrySplitTerminalHardBreak([NotNullWhen(true)] out TextLine? result)
    {
        int end = this.data.Count - 1;
        if (end <= 0 || !this.data[end].IsNewLine)
        {
            result = null;
            return false;
        }

        int graphemeIndex = this.data[end].GraphemeIndex;
        int start = end;
        while (start > 0 && this.data[start - 1].GraphemeIndex == graphemeIndex)
        {
            start--;
        }

        int count = this.data.Count - start;
        result = new TextLine(count);
        result.data.AddRange(this.data.GetRange(start, count));
        RecalculateLineMetrics(result);

        this.data.RemoveRange(start, count);
        RecalculateLineMetrics(this);
        return true;
    }

    /// <summary>
    /// Returns whether CSS <c>word-break: keep-all</c> suppresses the candidate break before
    /// the entry at <paramref name="index"/>.
    /// </summary>
    /// <remarks>
    /// See <see href="https://drafts.csswg.org/css-text-4/#word-break-property">CSS Text Module Level 4, word-break</see>.
    /// </remarks>
    /// <param name="index">The entry index immediately after the candidate break.</param>
    /// <returns><see langword="true"/> if the candidate break is within a keep-all word unit sequence.</returns>
    private bool IsKeepAllSuppressedBreak(int index)
    {
        if (index <= 0 || index >= this.data.Count)
        {
            return false;
        }

        return IsKeepAllWordUnit(this.data[index - 1].CodePoint)
            && IsKeepAllWordUnit(this.data[index].CodePoint);
    }

    /// <summary>
    /// Returns whether <paramref name="codePoint"/> participates in a CSS keep-all word unit
    /// sequence.
    /// </summary>
    /// <remarks>
    /// CSS <c>keep-all</c> uses typographic letter units and the Unicode line-breaking
    /// classes <c>NU</c>, <c>AL</c>, <c>AI</c>, and <c>ID</c>.
    /// See <see href="https://www.unicode.org/reports/tr14/#Line_Break_Property_Values">Unicode Standard Annex #14, Line Breaking Classes</see>.
    /// </remarks>
    /// <param name="codePoint">The code point to classify.</param>
    /// <returns><see langword="true"/> if the code point participates in a keep-all word unit sequence.</returns>
    private static bool IsKeepAllWordUnit(CodePoint codePoint)
        => CodePoint.IsLetter(codePoint)
        || CodePoint.IsNumber(codePoint)
        || CodePoint.GetLineBreakClass(codePoint) is
            LineBreakClass.Numeric
            or LineBreakClass.Alphabetic
            or LineBreakClass.Ambiguous
            or LineBreakClass.Ideographic;

    /// <summary>
    /// Finalizes this line after line-breaking: trims trailing breaking whitespace when requested,
    /// applies bidi reordering so entries are in visual order, and recomputes aggregated metrics.
    /// </summary>
    /// <param name="layoutMode">The orientation used to finalize directional run fragments.</param>
    /// <param name="skipJustification">
    /// When <see langword="true"/>, marks the line so <see cref="Justify"/> becomes a no-op
    /// (used for paragraph-final lines).
    /// </param>
    /// <param name="normalizeDecomposedAdvances">
    /// When <see langword="true"/>, moves decomposed grapheme advances to the final visual entry.
    /// </param>
    /// <param name="preserveTrailingBreakingWhitespace">
    /// When <see langword="true"/>, keeps ordinary trailing breaking whitespace in the finalized line.
    /// </param>
    /// <returns>This line, for fluent chaining.</returns>
    public TextLine Finalize(
        LayoutMode layoutMode,
        bool skipJustification = false,
        bool normalizeDecomposedAdvances = false,
        bool preserveTrailingBreakingWhitespace = false)
    {
        this.SkipJustification = skipJustification;
        this.RemoveTrailingBreakingWhitespace(preserveTrailingBreakingWhitespace);
        this.BidiReOrder(layoutMode);

        if (normalizeDecomposedAdvances)
        {
            this.NormalizeDecomposedAdvances();
        }

        RecalculateLineMetrics(this);
        return this;
    }

    /// <summary>
    /// Moves decomposed grapheme advances when bidi reordering moved the grapheme boundary marker.
    /// </summary>
    private void NormalizeDecomposedAdvances()
    {
        int start = 0;
        while (start < this.data.Count)
        {
            int graphemeIndex = this.data[start].GraphemeIndex;
            int end = start + 1;
            bool hasDecomposedEntry = this.data[start].IsDecomposed;

            while (end < this.data.Count && this.data[end].GraphemeIndex == graphemeIndex)
            {
                hasDecomposedEntry |= this.data[end].IsDecomposed;
                end++;
            }

            if (hasDecomposedEntry && end - start > 1 && !this.data[end - 1].IsLastInGrapheme)
            {
                float advance = 0;
                for (int i = start; i < end; i++)
                {
                    GlyphLayoutData glyph = this.data[i];
                    advance += glyph.ScaledAdvance;
                    glyph.ScaledAdvance = 0;
                    glyph.IsLastInGrapheme = false;
                    this.data[i] = glyph;
                }

                GlyphLayoutData last = this.data[end - 1];
                last.ScaledAdvance = advance;
                last.IsLastInGrapheme = true;
                this.data[end - 1] = last;
            }

            start = end;
        }
    }

    /// <summary>
    /// Distributes the remaining space between the line advance and the wrapping length across
    /// either inter-character or inter-word gaps, as configured by
    /// <see cref="TextOptions.TextJustification"/>.
    /// </summary>
    /// <remarks>
    /// No-op when the line was finalized with <c>skipJustification</c>, when wrapping is
    /// disabled, when no justification style is selected, or when the line is already at or
    /// beyond the wrapping length.
    /// </remarks>
    /// <param name="options">The text options supplying the wrapping length and justification style.</param>
    public void Justify(TextOptions options)
    {
        if (options.WrappingLength == -1F || options.TextJustification == TextJustification.None)
        {
            return;
        }

        if (this.ScaledLineAdvance == 0)
        {
            return;
        }

        float delta = (options.WrappingLength / options.Dpi) - this.ScaledLineAdvance;
        if (delta <= 0)
        {
            return;
        }

        // Increase the advance for all non zero-width glyphs but the last.
        if (options.TextJustification == TextJustification.InterCharacter)
        {
            int nonZeroCount = 0;
            for (int i = 0; i < this.data.Count; i++)
            {
                GlyphLayoutData glyph = this.data[i];
                if (!CodePoint.IsZeroWidthJoiner(glyph.CodePoint)
                    && !CodePoint.IsZeroWidthNonJoiner(glyph.CodePoint))
                {
                    nonZeroCount++;
                }
            }

            int opportunityCount = nonZeroCount - 1;
            if (opportunityCount == 0)
            {
                return;
            }

            float padding = delta / opportunityCount;
            int remainingOpportunities = opportunityCount;
            for (int i = 0; i < this.data.Count && remainingOpportunities > 0; i++)
            {
                GlyphLayoutData glyph = this.data[i];
                if (!CodePoint.IsZeroWidthJoiner(glyph.CodePoint)
                    && !CodePoint.IsZeroWidthNonJoiner(glyph.CodePoint))
                {
                    glyph.ScaledAdvance += padding;
                    this.data[i] = glyph;
                    remainingOpportunities--;
                }
            }

            RecalculateLineMetrics(this);
            return;
        }

        // Increase the advance for all spaces but the last.
        if (options.TextJustification == TextJustification.InterWord)
        {
            // Count all the whitespace characters.
            int whiteSpaceCount = 0;
            for (int i = 0; i < this.data.Count; i++)
            {
                GlyphLayoutData glyph = this.data[i];
                if (CodePoint.IsWhiteSpace(glyph.CodePoint))
                {
                    whiteSpaceCount++;
                }
            }

            if (whiteSpaceCount == 0)
            {
                return;
            }

            float padding = delta / whiteSpaceCount;
            for (int i = 0; i < this.data.Count; i++)
            {
                GlyphLayoutData glyph = this.data[i];
                if (CodePoint.IsWhiteSpace(glyph.CodePoint))
                {
                    glyph.ScaledAdvance += padding;
                    this.data[i] = glyph;
                }
            }
        }

        RecalculateLineMetrics(this);
    }

    /// <summary>
    /// Re-orders the entries in this line from logical to visual order according to the
    /// Unicode Bidirectional Algorithm (<see href="https://unicode.org/reports/tr9/"/>, rule L2).
    /// </summary>
    /// <param name="layoutMode">The orientation used to finalize directional run fragments.</param>
    public void BidiReOrder(LayoutMode layoutMode) => BidiReordering.Reorder(this.data, layoutMode);

    /// <summary>
    /// Recomputes the aggregated per-line metrics (advance, max line height, ascender,
    /// descender, delta, min-Y) from the current entries. Called after any mutation that
    /// can affect these — split, insert, trim, justify.
    /// </summary>
    /// <param name="textLine">The line to recompute metrics for.</param>
    private static void RecalculateLineMetrics(TextLine textLine)
    {
        // Lastly recalculate this line metrics.
        float advance = 0;
        float ascender = 0;
        float descender = 0;
        float delta = 0;
        float lineHeight = 0;
        float minY = 0;
        for (int i = 0; i < textLine.Count; i++)
        {
            GlyphLayoutData glyph = textLine[i];
            advance += glyph.ScaledAdvance;
            ascender = MathF.Max(ascender, glyph.ScaledAscender);
            descender = MathF.Max(descender, glyph.ScaledDescender);
            delta = MathF.Max(delta, glyph.ScaledDelta);
            lineHeight = MathF.Max(lineHeight, glyph.ScaledLineHeight);
            minY = MathF.Min(minY, glyph.ScaledMinY);
        }

        textLine.ScaledLineAdvance = advance;
        textLine.ScaledMaxAscender = ascender;
        textLine.ScaledMaxDescender = descender;
        textLine.ScaledMaxDelta = delta;
        textLine.ScaledMaxLineHeight = lineHeight;
        textLine.ScaledMinY = minY;
    }
}
