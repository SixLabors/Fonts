// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulates logic for laying out text.
/// </summary>
internal static partial class TextLayout
{
    /// <summary>
    /// Resolves the ordered sequence of <see cref="TextRun"/> instances that cover <paramref name="text"/>.
    /// </summary>
    /// <remarks>
    /// If <see cref="TextOptions.TextRuns"/> is <see langword="null"/> or empty, a single run covering the entire
    /// grapheme range of <paramref name="text"/> using <see cref="TextOptions.Font"/> is returned. Otherwise the
    /// supplied runs are ordered, gaps are filled with default-font runs, and overlapping ranges are trimmed.
    /// </remarks>
    /// <param name="text">The text to partition into runs.</param>
    /// <param name="options">The text options supplying the default font and optional user-defined runs.</param>
    /// <returns>The resolved runs that together cover the entire grapheme range of <paramref name="text"/>.</returns>
    public static IReadOnlyList<TextRun> BuildTextRuns(ReadOnlySpan<char> text, TextOptions options)
    {
        int start = 0;
        int end = text.GetGraphemeCount();
        if (end == 0)
        {
            return [];
        }

        if (options.TextRuns is null || options.TextRuns.Count == 0)
        {
            return new TextRun[]
            {
                new()
                {
                    Start = 0,
                    End = text.GetGraphemeCount(),
                    Font = options.Font
                }
            };
        }

        List<TextRun> textRuns = [];
        foreach (TextRun textRun in options.TextRuns.OrderBy(x => x.Start))
        {
            // Fill gaps within runs.
            if (textRun.Start > start)
            {
                textRuns.Add(new()
                {
                    Start = start,
                    End = textRun.Start,
                    Font = options.Font
                });
            }

            // Add the current run, ensuring the font is not null.
            textRun.Font ??= options.Font;

            if (textRun.Placeholder.HasValue && textRun.End != textRun.Start)
            {
                throw new ArgumentException("Placeholder text runs must be zero-length insertion runs.", nameof(options));
            }

            // Ensure that the previous run does not overlap the current.
            if (textRuns.Count > 0)
            {
                int prevIndex = textRuns.Count - 1;
                TextRun previous = textRuns[prevIndex];
                previous.End = Math.Min(previous.End, textRun.Start);
            }

            textRuns.Add(textRun);
            start = textRun.End;
        }

        // Add a final run if required.
        if (start < end)
        {
            textRuns.Add(new()
            {
                Start = start,
                End = end,
                Font = options.Font
            });
        }

        return textRuns;
    }

    /// <summary>
    /// Shapes <paramref name="text"/> into shaping state that is independent of the wrapping length.
    /// </summary>
    /// <remarks>
    /// Performs the font-run build, bidi analysis, GSUB/GPOS shaping (including fallback font
    /// resolution for unmapped codepoints). The result contains the positioned glyph collection
    /// and bidi state used by logical line composition.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The text options used while shaping.</param>
    /// <returns>The wrapping-independent shaping state.</returns>
    public static ShapedText ShapeText(ReadOnlySpan<char> text, TextOptions options)
    {
        // Gather the font and fallbacks.
        Font[] fallbackFonts = (options.FallbackFontFamilies?.Count > 0)
            ? [.. options.FallbackFontFamilies.Select(x => new Font(x, options.Font.Size, options.Font.RequestedStyle))]
            : [];

        LayoutMode layoutMode = options.LayoutMode;
        GlyphSubstitutionCollection substitutions = new(options);
        GlyphPositioningCollection positionings = new(options);

        // Analyse the text for bidi directional runs.
        BidiAlgorithm bidi = BidiAlgorithm.Instance.Value!;
        BidiData bidiData = new();
        bidiData.Init(text, (sbyte)options.TextDirection);

        if (options.TextBidiMode == TextBidiMode.Override)
        {
            BidiCharacterType overrideType = options.TextDirection == TextDirection.Auto
                ? (bidi.ResolveEmbeddingLevel(bidiData.Types) == 1 ? BidiCharacterType.RightToLeft : BidiCharacterType.LeftToRight)
                : (options.TextDirection == TextDirection.RightToLeft ? BidiCharacterType.RightToLeft : BidiCharacterType.LeftToRight);

            for (int i = 0; i < bidiData.Types.Length; i++)
            {
                // Bidi override is a higher-level protocol override: real text behaves as the requested
                // strong direction, while separators and explicit bidi controls keep their structural role.
                bidiData.Types[i] = bidiData.Types[i] switch
                {
                    BidiCharacterType.ParagraphSeparator
                    or BidiCharacterType.SegmentSeparator
                    or BidiCharacterType.BoundaryNeutral
                    or BidiCharacterType.LeftToRightEmbedding
                    or BidiCharacterType.RightToLeftEmbedding
                    or BidiCharacterType.LeftToRightOverride
                    or BidiCharacterType.RightToLeftOverride
                    or BidiCharacterType.PopDirectionalFormat
                    or BidiCharacterType.LeftToRightIsolate
                    or BidiCharacterType.RightToLeftIsolate
                    or BidiCharacterType.FirstStrongIsolate
                    or BidiCharacterType.PopDirectionalIsolate => bidiData.Types[i],
                    _ => overrideType,
                };
            }
        }

        bidi.Process(bidiData);

        // Get the list of directional runs
        BidiRun[] bidiRuns = [.. BidiRun.CoalesceLevels(bidi.ResolvedLevels)];
        Dictionary<int, int> bidiMap = [];

        // Incrementally build out collection of glyphs.
        IReadOnlyList<TextRun> textRuns = BuildTextRuns(text, options);

        // First do multiple font runs using the individual text runs.
        bool complete = true;
        int textRunIndex = 0;
        int codePointIndex = 0;
        int bidiRunIndex = 0;
        foreach (TextRun textRun in textRuns)
        {
            if (textRun.Placeholder.HasValue)
            {
                substitutions.Clear();

                while (bidiRunIndex < bidiRuns.Length && codePointIndex == bidiRuns[bidiRunIndex].End)
                {
                    bidiRunIndex++;
                }

                // Placeholder direction comes from the bidi region at the insertion
                // point. If the insertion point is after all source text, use the
                // default even/LTR embedding level.
                BidiRun placeholderBidiRun = bidiRunIndex < bidiRuns.Length
                    ? bidiRuns[bidiRunIndex]
                    : new(BidiCharacterType.LeftToRight, 2, codePointIndex, 0);

                // Placeholder runs are inserted into the layout stream and do not consume
                // source graphemes, source codepoints, or bidi runs.
                substitutions.AddPlaceholder(
                    CodePoint.ObjectReplacementChar,
                    placeholderBidiRun,
                    textRun,
                    codePointIndex);

                complete &= positionings.TryAdd(textRun.Font!, substitutions);
                textRunIndex++;
                continue;
            }

            if (!DoFontRun(
                textRun.Slice(text),
                textRun.Start,
                textRuns,
                ref textRunIndex,
                ref codePointIndex,
                ref bidiRunIndex,
                false,
                textRun.Font!,
                bidiRuns,
                bidiMap,
                substitutions,
                positionings))
            {
                complete = false;
            }
        }

        if (!complete)
        {
            // Finally try our fallback fonts.
            // We do a complete run here across the whole collection.
            foreach (Font font in fallbackFonts)
            {
                textRunIndex = 0;
                codePointIndex = 0;
                bidiRunIndex = 0;
                if (DoFontRun(
                    text,
                    0,
                    textRuns,
                    ref textRunIndex,
                    ref codePointIndex,
                    ref bidiRunIndex,
                    true,
                    font,
                    bidiRuns,
                    bidiMap,
                    substitutions,
                    positionings))
                {
                    break;
                }
            }
        }

        // Update the positions of the glyphs in the completed collection.
        // Each set of metrics is associated with single font and will only be updated
        // by that font so it's safe to use a single collection.
        Font? lastFont = null;
        for (int i = 0; i < textRuns.Count; i++)
        {
            TextRun textRun = textRuns[i];

            if (textRun.Font == lastFont)
            {
                continue;
            }

            textRun.Font!.FontMetrics.UpdatePositions(positionings);
            lastFont = textRun.Font;
        }

        foreach (Font font in fallbackFonts)
        {
            font.FontMetrics.UpdatePositions(positionings);
        }

        return new ShapedText(positionings, bidiRuns, bidiMap, layoutMode);
    }

    /// <summary>
    /// Lays out the supplied <see cref="TextBox"/>, streaming each laid-out glyph through the
    /// supplied <paramref name="visitor"/> in layout order using the supplied wrapping length for alignment.
    /// </summary>
    /// <remarks>
    /// The visitor type is constrained to a struct implementing <see cref="IGlyphLayoutVisitor"/>
    /// so the JIT specializes dispatch per visitor — no boxing or delegate allocation.
    /// </remarks>
    /// <typeparam name="TVisitor">The concrete visitor struct type.</typeparam>
    /// <param name="textBox">The shaped and line-broken text.</param>
    /// <param name="options">The text options used to lay out <paramref name="textBox"/>.</param>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <param name="visitor">The visitor that receives each positioned glyph.</param>
    public static void LayoutText<TVisitor>(
        TextBox textBox,
        TextOptions options,
        float wrappingLength,
        ref TVisitor visitor)
        where TVisitor : struct, IGlyphLayoutVisitor
    {
        if (textBox.TextLines.Count == 0)
        {
            return;
        }

        LayoutMode layoutMode = options.LayoutMode;

        Vector2 boxLocation = options.Origin / options.Dpi;
        Vector2 penLocation = boxLocation;

        // When wrapping is enabled, the wrapping length defines the minimum line-box
        // extent used by alignment.
        float maxScaledAdvance = textBox.ScaledMaxAdvance();
        if (options.TextAlignment != TextAlignment.Start && wrappingLength > 0)
        {
            maxScaledAdvance = Math.Max(wrappingLength / options.Dpi, maxScaledAdvance);
        }

        TextDirection direction = textBox.TextDirection();

        if (layoutMode == LayoutMode.HorizontalTopBottom)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                visitor.BeginLine(i);
                LayoutLineHorizontal(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    ref boxLocation,
                    ref penLocation,
                    ref visitor);

                visitor.EndLine();
            }
        }
        else if (layoutMode == LayoutMode.HorizontalBottomTop)
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                visitor.BeginLine(i);
                LayoutLineHorizontal(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    ref boxLocation,
                    ref penLocation,
                    ref visitor);

                visitor.EndLine();
            }
        }
        else if (layoutMode is LayoutMode.VerticalLeftRight)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                visitor.BeginLine(i);
                LayoutLineVertical(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    ref boxLocation,
                    ref penLocation,
                    ref visitor);

                visitor.EndLine();
            }
        }
        else if (layoutMode is LayoutMode.VerticalRightLeft)
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                visitor.BeginLine(i);
                LayoutLineVertical(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    ref boxLocation,
                    ref penLocation,
                    ref visitor);

                visitor.EndLine();
            }
        }
        else if (layoutMode is LayoutMode.VerticalMixedLeftRight)
        {
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                visitor.BeginLine(i);
                LayoutLineVerticalMixed(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    i,
                    ref boxLocation,
                    ref penLocation,
                    ref visitor);

                visitor.EndLine();
            }
        }
        else
        {
            int index = 0;
            for (int i = textBox.TextLines.Count - 1; i >= 0; i--)
            {
                visitor.BeginLine(i);
                LayoutLineVerticalMixed(
                    textBox,
                    textBox.TextLines[i],
                    direction,
                    maxScaledAdvance,
                    options,
                    index++,
                    ref boxLocation,
                    ref penLocation,
                    ref visitor);

                visitor.EndLine();
            }
        }
    }

    /// <summary>
    /// Positions one line of horizontal text. Applies vertical-block alignment (on the first line),
    /// horizontal-block alignment, per-line text alignment, and any first-line ink-overshoot
    /// compensation, then streams each positioned glyph through <paramref name="visitor"/>.
    /// </summary>
    /// <typeparam name="TVisitor">The concrete visitor struct type.</typeparam>
    /// <param name="textBox">The containing text box (used to look up sibling lines for block alignment).</param>
    /// <param name="textLine">The line being laid out.</param>
    /// <param name="direction">The resolved text direction for this line.</param>
    /// <param name="maxScaledAdvance">The widest scaled line advance in the block (or wrapping length).</param>
    /// <param name="options">The text options used to position the line.</param>
    /// <param name="index">The zero-based visual index of this line within the block.</param>
    /// <param name="boxLocation">The running top-left position of the glyph boxes; advanced by this method.</param>
    /// <param name="penLocation">The running pen position used for glyph placement; advanced by this method.</param>
    /// <param name="visitor">The visitor that receives each positioned glyph.</param>
    private static void LayoutLineHorizontal<TVisitor>(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        ref Vector2 boxLocation,
        ref Vector2 penLocation,
        ref TVisitor visitor)
        where TVisitor : struct, IGlyphLayoutVisitor
    {
        // Offset the location to center the line vertically.
        bool isFirstLine = index == 0;
        float scaledLineHeight = textLine.ScaledMaxLineHeight;

        // Recover the unscaled line height to calculate proper centering
        float unscaledLineHeight = scaledLineHeight / options.LineSpacing;
        float advanceY = scaledLineHeight;

        // Center the glyphs within the extra space created by LineSpacing
        float offsetY = (advanceY - unscaledLineHeight) * .5F;
        float yLineAdvance = advanceY - offsetY;

        float originX = penLocation.X;
        float offsetX = 0;

        // Set the Y origin for the first horizontal line and account for tall stacks.
        if (isFirstLine)
        {
            // ScaledMinY is the minimum ink Y for this line in Y down (baseline at 0).
            // -ScaledMinY is the actual ascent required to contain the ink.
            // ScaledMaxAscender is the typographic ascent we already used to build the line box.
            float requiredAscent = -textLine.ScaledMinY;
            float extraAscent = requiredAscent - textLine.ScaledMaxAscender;

            if (extraAscent > 0)
            {
                // Shift the baseline down only by the extra ascent needed so that
                // stacked glyphs (Tibetan, etc) fit inside the bitmap. For Latin,
                // requiredAscent ~= ScaledMaxAscender and extraAscent is zero.
                offsetY += extraAscent;
                advanceY += extraAscent;
            }

            switch (options.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetY -= textBox.TextLines[i].ScaledMaxLineHeight * .5F;
                    }

                    break;
                case VerticalAlignment.Bottom:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetY -= textBox.TextLines[i].ScaledMaxLineHeight;
                    }

                    break;
            }
        }

        penLocation.Y += offsetY;

        // Set the X-Origin for horizontal alignment.
        switch (options.HorizontalAlignment)
        {
            case HorizontalAlignment.Right:
                offsetX = -maxScaledAdvance;
                break;
            case HorizontalAlignment.Center:
                offsetX = -(maxScaledAdvance * .5F);
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.End:
                    offsetX += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.Start:
                    offsetX += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }

        penLocation.X += offsetX;
        Vector2 boundsLocation = boxLocation;

        bool emitted = false;
        for (int i = 0; i < textLine.Count; i++)
        {
            GlyphLayoutData data = textLine[i];
            float layoutAdvance = data.ScaledAdvance;

            if (data.IsNewLine)
            {
                FontGlyphMetrics metric = data.Metrics[0];

                // Hard breaks bypass the normal glyph loop, but still need the
                // current pen position plus the same baseline origin used by glyphs.
                Vector2 hardBreakGlyphOrigin = penLocation + new Vector2(0, textLine.ScaledMaxAscender);

                visitor.Visit(
                    new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    data.Font,
                    boundsLocation,
                    hardBreakGlyphOrigin,
                    penLocation,
                    data.ScaledAdvance,
                    yLineAdvance,
                    GlyphLayoutMode.Horizontal,
                    data.BidiRun.Level,
                    true,
                    data.GraphemeIndex,
                    data.StringIndex));

                penLocation.X = originX;
                penLocation.Y += yLineAdvance;
                boxLocation.X = originX;
                boxLocation.Y += advanceY;
                boundsLocation.X = originX;
                boundsLocation.Y += advanceY;
                return;
            }

            int j = 0;
            foreach (FontGlyphMetrics metric in data.Metrics)
            {
                Vector2 glyphOrigin = penLocation + new Vector2(0, textLine.ScaledMaxAscender);

                visitor.Visit(
                    new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    data.Font,
                    boundsLocation,
                    glyphOrigin,
                    glyphOrigin,
                    data.ScaledAdvance,
                    advanceY,
                    GlyphLayoutMode.Horizontal,
                    data.BidiRun.Level,
                    i == 0 && j == 0,
                    data.GraphemeIndex,
                    data.StringIndex));

                emitted = true;
                j++;
            }

            boxLocation.X += layoutAdvance;
            penLocation.X += layoutAdvance;
            boundsLocation.X += data.ScaledAdvance;
        }

        boxLocation.X = originX;
        penLocation.X = originX;
        if (emitted)
        {
            penLocation.Y += yLineAdvance;
            boxLocation.Y += advanceY;
        }
    }

    /// <summary>
    /// Positions one line of vertical text (<see cref="LayoutMode.VerticalLeftRight"/> and
    /// <see cref="LayoutMode.VerticalRightLeft"/>). All glyphs are treated as naturally vertical —
    /// transformed (rotated) graphemes receive grapheme-level horizontal centering based on the
    /// collective ink width of every entry sharing a grapheme index.
    /// </summary>
    /// <typeparam name="TVisitor">The concrete visitor struct type.</typeparam>
    /// <param name="textBox">The containing text box (used to look up sibling lines for block alignment).</param>
    /// <param name="textLine">The line being laid out.</param>
    /// <param name="direction">The resolved text direction for this line.</param>
    /// <param name="maxScaledAdvance">The longest scaled line advance in the block (or wrapping length).</param>
    /// <param name="options">The text options used to position the line.</param>
    /// <param name="index">The zero-based visual index of this line within the block.</param>
    /// <param name="boxLocation">The running top-left position of the glyph boxes; advanced by this method.</param>
    /// <param name="penLocation">The running pen position used for glyph placement; advanced by this method.</param>
    /// <param name="visitor">The visitor that receives each positioned glyph.</param>
    private static void LayoutLineVertical<TVisitor>(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        ref Vector2 boxLocation,
        ref Vector2 penLocation,
        ref TVisitor visitor)
        where TVisitor : struct, IGlyphLayoutVisitor
    {
        float originY = penLocation.Y;
        float offsetY = 0;

        // Offset the location to center the line horizontally.
        float scaledMaxLineHeight = textLine.ScaledMaxLineHeight;

        // Recover the unscaled line height to calculate proper centering
        float unscaledLineHeight = scaledMaxLineHeight / options.LineSpacing;
        float advanceX = scaledMaxLineHeight;

        // Center the glyphs within the extra space created by LineSpacing
        float offsetX = (advanceX - unscaledLineHeight) * .5F;
        float xLineAdvance = advanceX - offsetX;

        // Set the Y-Origin for the line.
        switch (options.VerticalAlignment)
        {
            case VerticalAlignment.Top:
                offsetY = 0;
                break;
            case VerticalAlignment.Center:
                offsetY -= maxScaledAdvance * .5F;
                break;
            case VerticalAlignment.Bottom:
                offsetY -= maxScaledAdvance;
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.End:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.Start:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }

        bool isFirstLine = index == 0;
        if (isFirstLine)
        {
            // In vertical layout, first-line Y ascent compensation introduces unwanted
            // leading space before the first glyph. Keep first-line handling limited
            // to X-origin block alignment only.

            // Set the X-Origin for horizontal alignment.
            switch (options.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight;
                    }

                    break;
                case HorizontalAlignment.Center:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight * .5F;
                    }

                    break;
            }
        }

        penLocation.Y += offsetY;
        penLocation.X += offsetX;

        float lineOriginX = penLocation.X;
        Vector2 boundsLocation = boxLocation;
        float boundsLineOriginX = boundsLocation.X;

        bool emitted = false;

        // Grapheme-scoped state for transformed glyph alignment.
        //
        // IMPORTANT: GlyphLayoutData is per-codepoint, not per-grapheme.
        // Complex scripts can therefore produce multiple entries for a single grapheme.
        // For example Devanagari "र्कि" can end up as two entries ("र्" and "कि") even though it
        // visually shapes as a single cluster.
        //
        // - Compute a single alignX for the whole grapheme (across all entries with the same GraphemeIndex).
        // - Apply that alignX as a positional offset only, never as part of pen/box advance.
        // - Transformed entries still advance along X within the grapheme (horizontal glyphs inside a vertical flow),
        //   then X is reset at the end of the grapheme.
        float currentGraphemeAlignX = 0;
        bool currentGraphemeIsTransformed = false;

        for (int i = 0; i < textLine.Count; i++)
        {
            GlyphLayoutData data = textLine[i];
            float layoutAdvance = data.ScaledAdvance;
            float scaledLineHeight = data.ScaledLineHeight / options.LineSpacing;

            if (data.IsNewLine)
            {
                FontGlyphMetrics metric = data.Metrics[0];
                Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;

                // Hard breaks bypass the normal glyph loop, but still need the
                // current pen position plus the same vertical glyph origin adjustment.
                Vector2 hardBreakDecorationOrigin = penLocation + new Vector2((unscaledLineHeight - scaledLineHeight) * .5F, 0);
                Vector2 hardBreakGlyphOrigin = hardBreakDecorationOrigin + new Vector2(0, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);

                visitor.Visit(
                    new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    data.Font,
                    boundsLocation,
                    hardBreakGlyphOrigin,
                    hardBreakDecorationOrigin,
                    xLineAdvance,
                    data.ScaledAdvance,
                    GlyphLayoutMode.Vertical,
                    data.BidiRun.Level,
                    true,
                    data.GraphemeIndex,
                    data.StringIndex));

                boxLocation.X += advanceX;
                boxLocation.Y = originY;
                penLocation.X += xLineAdvance;
                penLocation.Y = originY;
                boundsLocation.X += advanceX;
                boundsLocation.Y = originY;
                return;
            }

            int j = 0;

            bool isFirstInGrapheme = data.GraphemeCodePointIndex == 0;
            float alignX = 0;
            float entryScaledAdvanceWidth = 0;

            if (isFirstInGrapheme)
            {
                // Reset grapheme-scoped state at the start of each grapheme.
                currentGraphemeAlignX = 0;
                currentGraphemeIsTransformed = false;

                // Determine whether this grapheme contains any transformed entries.
                // This is intentionally done at grapheme scope because individual entries can differ.
                int graphemeIndex = data.GraphemeIndex;

                for (int k = i; k < textLine.Count; k++)
                {
                    GlyphLayoutData g = textLine[k];

                    if (g.GraphemeIndex != graphemeIndex)
                    {
                        break;
                    }

                    if (g.IsTransformed)
                    {
                        currentGraphemeIsTransformed = true;
                        break;
                    }
                }

                if (currentGraphemeIsTransformed)
                {
                    // In vertical layout, glyphs with a vertical orientation of TransformRotate/TransformUpright are
                    // rendered as "horizontal" glyphs inside a vertical flow.
                    //
                    // Their horizontal metrics (including LSB) are still expressed in the font's horizontal writing mode,
                    // so without an adjustment these glyphs appear shifted within the column.
                    //
                    // To make transformed glyphs align visually with naturally-vertical glyphs, we center the ink bounds
                    // of the ENTIRE grapheme (across all entries with the same GraphemeIndex) within the column width
                    // (`scaledMaxLineHeight`).
                    float minX = float.PositiveInfinity;
                    float maxX = float.NegativeInfinity;

                    for (int k = i; k < textLine.Count; k++)
                    {
                        GlyphLayoutData g = textLine[k];

                        if (g.GraphemeIndex != graphemeIndex)
                        {
                            break;
                        }

                        foreach (FontGlyphMetrics m in g.Metrics)
                        {
                            Vector2 s = new Vector2(g.PointSize) / m.ScaleFactor;

                            float glyphMinX = m.Bounds.Min.X * s.X;
                            float glyphMaxX = m.Bounds.Max.X * s.X;

                            if (glyphMinX < minX)
                            {
                                minX = glyphMinX;
                            }

                            if (glyphMaxX > maxX)
                            {
                                maxX = glyphMaxX;
                            }
                        }
                    }

                    float inkWidth = maxX - minX;

                    // Normalize ink minX to 0 and center within the entry's own line box.
                    // The decoration origin has already centered that entry line box within
                    // the widest line box, so using the widest line box here would apply the
                    // mixed-size offset twice.
                    // This is grapheme-correct and avoids centering based only on the "first" entry,
                    // which is not representative for marks like reph in Devanagari.
                    currentGraphemeAlignX = -minX + ((scaledLineHeight - inkWidth) * .5F);
                }
            }

            if (currentGraphemeIsTransformed)
            {
                // Apply the grapheme-level horizontal centering offset to every entry in the grapheme.
                // This is positional only and must never be folded into any advance.
                alignX = currentGraphemeAlignX;

                // Transformed glyphs are still positioned using horizontal metrics (`AdvanceWidth`) even though
                // they participate in a vertical flow. `AdvanceWidth` gives us the horizontal pen advance we must
                // apply between entries inside the transformed grapheme.
                foreach (FontGlyphMetrics m in data.Metrics)
                {
                    Vector2 s = new Vector2(data.PointSize) / m.ScaleFactor;
                    entryScaledAdvanceWidth += m.AdvanceWidth * s.X;
                }
            }

            foreach (FontGlyphMetrics metric in data.Metrics)
            {
                // Align the glyph horizontally and vertically centering vertically around the baseline.
                Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;
                float glyphAlignX = alignX;

                if (!currentGraphemeIsTransformed)
                {
                    // Vertical origin fallback places the vertical origin at half the
                    // horizontal advance. The decoration origin has already centered this
                    // entry's line box in the column, so center the glyph advance inside it.
                    glyphAlignX = (scaledLineHeight - (metric.AdvanceWidth * scale.X)) * .5F;
                }

                // Move the glyph origin without changing the advance or decoration origin.
                Vector2 glyphOffset = new(glyphAlignX, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);
                Vector2 decorationOrigin = penLocation + new Vector2((unscaledLineHeight - scaledLineHeight) * .5F, 0);
                Vector2 glyphOrigin = decorationOrigin + glyphOffset;

                float advanceW = advanceX;

                if (currentGraphemeIsTransformed && !isFirstInGrapheme)
                {
                    // For transformed glyphs after the first in the grapheme we advance
                    // horizontally using the horizontal advance not the line height.
                    // This gives us the correct total advance across the grapheme.
                    advanceW = scale.X * metric.AdvanceWidth;
                }

                visitor.Visit(
                    new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    data.Font,
                    boundsLocation,
                    glyphOrigin,
                    decorationOrigin,
                    advanceW,
                    data.ScaledAdvance,
                    GlyphLayoutMode.Vertical,
                    data.BidiRun.Level,
                    i == 0 && j == 0,
                    data.GraphemeIndex,
                    data.StringIndex));

                emitted = true;
                j++;
            }

            if (currentGraphemeIsTransformed)
            {
                // Advance horizontally between entries inside the transformed grapheme.
                boxLocation.X += entryScaledAdvanceWidth;
                penLocation.X += entryScaledAdvanceWidth;
            }

            if (currentGraphemeIsTransformed)
            {
                boundsLocation.X += entryScaledAdvanceWidth;
            }

            if (data.IsLastInGrapheme)
            {
                penLocation.Y += layoutAdvance;
                boxLocation.X = lineOriginX;
                penLocation.X = lineOriginX;
                boundsLocation.Y += data.ScaledAdvance;
                boundsLocation.X = boundsLineOriginX;
            }
        }

        boxLocation.Y = originY;
        penLocation.Y = originY;
        if (emitted)
        {
            boxLocation.X += advanceX;
            penLocation.X += xLineAdvance;
        }
    }

    /// <summary>
    /// Positions one line of vertical-mixed text (<see cref="LayoutMode.VerticalMixedLeftRight"/>
    /// and <see cref="LayoutMode.VerticalMixedRightLeft"/>). Transformed entries are rotated 90°
    /// and laid out sideways using the font's horizontal metrics while the pen still advances
    /// along Y; naturally-vertical entries are positioned using their vertical metrics.
    /// </summary>
    /// <typeparam name="TVisitor">The concrete visitor struct type.</typeparam>
    /// <param name="textBox">The containing text box (used to look up sibling lines for block alignment).</param>
    /// <param name="textLine">The line being laid out.</param>
    /// <param name="direction">The resolved text direction for this line.</param>
    /// <param name="maxScaledAdvance">The longest scaled line advance in the block (or wrapping length).</param>
    /// <param name="options">The text options used to position the line.</param>
    /// <param name="index">The zero-based visual index of this line within the block.</param>
    /// <param name="boxLocation">The running top-left position of the glyph boxes; advanced by this method.</param>
    /// <param name="penLocation">The running pen position used for glyph placement; advanced by this method.</param>
    /// <param name="visitor">The visitor that receives each positioned glyph.</param>
    private static void LayoutLineVerticalMixed<TVisitor>(
        TextBox textBox,
        TextLine textLine,
        TextDirection direction,
        float maxScaledAdvance,
        TextOptions options,
        int index,
        ref Vector2 boxLocation,
        ref Vector2 penLocation,
        ref TVisitor visitor)
        where TVisitor : struct, IGlyphLayoutVisitor
    {
        float originY = penLocation.Y;
        float offsetY = 0;

        // Offset the location to center the line horizontally.
        float scaledMaxLineHeight = textLine.ScaledMaxLineHeight;

        // Recover the unscaled line height to calculate proper centering
        float unscaledLineHeight = scaledMaxLineHeight / options.LineSpacing;
        float advanceX = scaledMaxLineHeight;

        // Center the glyphs within the extra space created by LineSpacing
        float offsetX = (advanceX - unscaledLineHeight) * .5F;
        float xLineAdvance = advanceX - offsetX;

        // Set the Y-Origin for the line.
        switch (options.VerticalAlignment)
        {
            case VerticalAlignment.Top:
                offsetY = 0;
                break;
            case VerticalAlignment.Center:
                offsetY -= maxScaledAdvance * .5F;
                break;
            case VerticalAlignment.Bottom:
                offsetY -= maxScaledAdvance;
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.End:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (options.TextAlignment)
            {
                case TextAlignment.Start:
                    offsetY += maxScaledAdvance - textLine.ScaledLineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (textLine.ScaledLineAdvance * .5F);
                    break;
            }
        }

        bool isFirstLine = index == 0;
        if (isFirstLine)
        {
            // In vertical-mixed layout, first-line Y ascent compensation introduces
            // unwanted leading space before the first glyph. Keep first-line handling
            // limited to X-origin block alignment only.

            // Set the X-Origin for horizontal alignment.
            switch (options.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight;
                    }

                    break;
                case HorizontalAlignment.Center:
                    for (int i = 0; i < textBox.TextLines.Count; i++)
                    {
                        offsetX -= textBox.TextLines[i].ScaledMaxLineHeight * .5F;
                    }

                    break;
            }
        }

        penLocation.Y += offsetY;
        penLocation.X += offsetX;
        Vector2 boundsLocation = boxLocation;

        bool emitted = false;
        for (int i = 0; i < textLine.Count; i++)
        {
            GlyphLayoutData data = textLine[i];
            float layoutAdvance = data.ScaledAdvance;
            float scaledLineHeight = data.ScaledLineHeight / options.LineSpacing;

            if (data.IsNewLine)
            {
                FontGlyphMetrics metric = data.Metrics[0];
                Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;

                // Hard breaks bypass the normal glyph loop, but still need the
                // current pen position plus the same vertical glyph origin adjustment.
                Vector2 hardBreakDecorationOrigin = penLocation + new Vector2((unscaledLineHeight - scaledLineHeight) * .5F, 0);
                Vector2 hardBreakGlyphOrigin = hardBreakDecorationOrigin + new Vector2(0, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);

                visitor.Visit(
                    new GlyphLayout(
                    new Glyph(metric, data.PointSize),
                    data.Font,
                    boundsLocation,
                    hardBreakGlyphOrigin,
                    hardBreakDecorationOrigin,
                    xLineAdvance,
                    data.ScaledAdvance,
                    GlyphLayoutMode.Vertical,
                    data.BidiRun.Level,
                    true,
                    data.GraphemeIndex,
                    data.StringIndex));

                boxLocation.X += advanceX;
                boxLocation.Y = originY;
                penLocation.X += xLineAdvance;
                penLocation.Y = originY;
                boundsLocation.X += advanceX;
                boundsLocation.Y = originY;
                return;
            }

            if (data.IsTransformed)
            {
                int j = 0;
                foreach (FontGlyphMetrics metric in data.Metrics)
                {
                    // The glyph will be rotated 90 degrees for vertical mixed layout.
                    // We still advance along Y, but the glyphs are laid out sideways in X.

                    // Calculate the initial horizontal offset to center the glyph baseline:
                    // - Take half the difference between the max line height (scaledMaxLineHeight)
                    //   and the current glyph's line height (data.ScaledLineHeight).
                    // - The line height includes both ascender and descender metrics.
                    float baselineDelta = (unscaledLineHeight - scaledLineHeight) * .5F;

                    // Adjust the horizontal offset further by considering the descender differences:
                    // - Subtract the current glyph's descender (data.ScaledDescender) to align it properly.
                    float descenderAbs = Math.Abs(data.ScaledDescender);
                    float descenderDelta = (Math.Abs(textLine.ScaledMaxDescender) - descenderAbs) * .5F;

                    float centerOffsetX = baselineDelta + descenderAbs + descenderDelta;
                    Vector2 glyphOrigin = penLocation + new Vector2(centerOffsetX, 0);

                    visitor.Visit(
                        new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        data.Font,
                        boundsLocation,
                        glyphOrigin,
                        glyphOrigin,
                        advanceX,
                        data.ScaledAdvance,
                        GlyphLayoutMode.VerticalRotated,
                        data.BidiRun.Level,
                        i == 0 && j == 0,
                        data.GraphemeIndex,
                        data.StringIndex));

                    emitted = true;
                    j++;
                }
            }
            else
            {
                int j = 0;
                foreach (FontGlyphMetrics metric in data.Metrics)
                {
                    // Align the glyph horizontally and vertically centering vertically around the baseline.
                    Vector2 scale = new Vector2(data.PointSize) / metric.ScaleFactor;

                    // Vertical origin fallback places the vertical origin at half the
                    // horizontal advance. The decoration origin has already centered this
                    // entry's line box in the column, so center the glyph advance inside it.
                    float glyphAlignX = (scaledLineHeight - (metric.AdvanceWidth * scale.X)) * .5F;
                    Vector2 glyphOffset = new(glyphAlignX, (metric.Bounds.Max.Y + metric.TopSideBearing) * scale.Y);
                    Vector2 decorationOrigin = penLocation + new Vector2((unscaledLineHeight - scaledLineHeight) * .5F, 0);
                    Vector2 glyphOrigin = decorationOrigin + glyphOffset;

                    visitor.Visit(
                        new GlyphLayout(
                        new Glyph(metric, data.PointSize),
                        data.Font,
                        boundsLocation,
                        glyphOrigin,
                        decorationOrigin,
                        advanceX,
                        data.ScaledAdvance,
                        GlyphLayoutMode.Vertical,
                        data.BidiRun.Level,
                        i == 0 && j == 0,
                        data.GraphemeIndex,
                        data.StringIndex));

                    emitted = true;
                    j++;
                }
            }

            penLocation.Y += layoutAdvance;
            boundsLocation.Y += data.ScaledAdvance;
        }

        boxLocation.Y = originY;
        penLocation.Y = originY;
        if (emitted)
        {
            boxLocation.X += advanceX;
            penLocation.X += xLineAdvance;
        }
    }

    /// <summary>
    /// Shapes a single font run — maps codepoints in <paramref name="text"/> to glyph ids using
    /// <paramref name="font"/>, then runs GSUB substitution and GPOS positioning. Codepoints that
    /// the font cannot map are recorded for a later fallback pass.
    /// </summary>
    /// <param name="text">The run-relative text slice to shape.</param>
    /// <param name="start">The starting grapheme index (absolute within the original input).</param>
    /// <param name="textRuns">The ordered list of resolved text runs.</param>
    /// <param name="textRunIndex">The index of the current text run; advanced as the enumerator crosses run boundaries.</param>
    /// <param name="codePointIndex">The running codepoint index (absolute within the original input).</param>
    /// <param name="bidiRunIndex">The running bidi run index.</param>
    /// <param name="isFallbackRun">
    /// <see langword="true"/> if this call is the fallback-font pass (in which case unmapped codepoints
    /// may still emit <c>.notdef</c> glyphs).
    /// </param>
    /// <param name="font">The font to shape with.</param>
    /// <param name="bidiRuns">The resolved bidi runs covering the whole input.</param>
    /// <param name="bidiMap">A codepoint → bidi-run mapping accumulated across shaping passes.</param>
    /// <param name="substitutions">The GSUB substitution collection to write into.</param>
    /// <param name="positionings">The GPOS positioning collection to write into.</param>
    /// <returns>
    /// <see langword="true"/> if every codepoint mapped successfully; <see langword="false"/> if any
    /// codepoint remains unmapped (so a fallback-font pass is needed).
    /// </returns>
    private static bool DoFontRun(
        ReadOnlySpan<char> text,
        int start,
        IReadOnlyList<TextRun> textRuns,
        ref int textRunIndex,
        ref int codePointIndex,
        ref int bidiRunIndex,
        bool isFallbackRun,
        Font font,
        BidiRun[] bidiRuns,
        Dictionary<int, int> bidiMap,
        GlyphSubstitutionCollection substitutions,
        GlyphPositioningCollection positionings)
    {
        // For each run we start with a fresh substitution collection to avoid
        // overwriting the glyph ids.
        substitutions.Clear();

        // Enumerate through each grapheme in the text.
        int graphemeIndex = start;
        SpanGraphemeEnumerator graphemeEnumerator = new(text);
        while (graphemeEnumerator.MoveNext())
        {
            ReadOnlySpan<char> grapheme = graphemeEnumerator.Current.Span;
            int graphemeMax = grapheme.Length - 1;
            int graphemeCodePointIndex = 0;
            int charIndex = 0;

            while (textRunIndex < textRuns.Count - 1 && graphemeIndex == textRuns[textRunIndex].End)
            {
                textRunIndex++;
            }

            // Now enumerate through each codepoint in the grapheme.
            bool skipNextCodePoint = false;
            SpanCodePointEnumerator codePointEnumerator = new(grapheme);
            while (codePointEnumerator.MoveNext())
            {
                if (codePointIndex == bidiRuns[bidiRunIndex].End)
                {
                    bidiRunIndex++;
                }

                if (skipNextCodePoint)
                {
                    codePointIndex++;
                    graphemeCodePointIndex++;
                    continue;
                }

                bidiMap[codePointIndex] = bidiRunIndex;

                int charsConsumed = 0;
                CodePoint current = codePointEnumerator.Current;
                charIndex += current.Utf16SequenceLength;
                CodePoint? next = graphemeCodePointIndex < graphemeMax
                    ? CodePoint.DecodeFromUtf16At(grapheme, charIndex, out charsConsumed)
                    : null;

                charIndex += charsConsumed;

                // Get the glyph id for the codepoint and add to the collection.
                bool hasGlyph = font.FontMetrics.TryGetGlyphId(current, next, out ushort glyphId, out skipNextCodePoint);

                // Unsupported default-ignorable code points such as FE0F should not block
                // GSUB sequences like emoji ZWJ ligatures. Preserve joiners explicitly.
                if (!hasGlyph &&
                    UnicodeUtility.IsDefaultIgnorableCodePoint((uint)current.Value) &&
                    !UnicodeUtility.ShouldRenderWhiteSpaceOnly(current) &&
                    !CodePoint.IsZeroWidthJoiner(current) &&
                    !CodePoint.IsZeroWidthNonJoiner(current))
                {
                    codePointIndex++;
                    graphemeCodePointIndex++;
                    continue;
                }

                substitutions.AddGlyph(glyphId, current, (TextDirection)bidiRuns[bidiRunIndex].Direction, textRuns[textRunIndex], codePointIndex);

                codePointIndex++;
                graphemeCodePointIndex++;
            }

            graphemeIndex++;
        }

        // Apply the simple and complex substitutions.
        // TODO: Investigate HarfBuzz normalizer.
        SubstituteBidiMirrors(font.FontMetrics, substitutions);
        font.FontMetrics.ApplySubstitution(substitutions);

        return !isFallbackRun
            ? positionings.TryAdd(font, substitutions)
            : positionings.TryUpdate(font, substitutions);
    }

    /// <summary>
    /// Substitutes mirrored bracket glyphs (for example <c>(</c> ↔ <c>)</c>) inside right-to-left
    /// bidi runs, per Unicode Bidirectional Algorithm rule L4. Relies on the font's <c>rtlm</c>
    /// feature when available and falls back to the Unicode mirror table otherwise.
    /// </summary>
    /// <param name="fontMetrics">The font metrics used to look up mirrored glyph ids.</param>
    /// <param name="collection">The substitution collection whose glyphs will be rewritten in place.</param>
    private static void SubstituteBidiMirrors(FontMetrics fontMetrics, GlyphSubstitutionCollection collection)
    {
        for (int i = 0; i < collection.Count; i++)
        {
            GlyphShapingData data = collection[i];

            if (data.Direction != TextDirection.RightToLeft)
            {
                continue;
            }

            if (!CodePoint.TryGetBidiMirror(data.CodePoint, out CodePoint mirror))
            {
                continue;
            }

            if (fontMetrics.TryGetGlyphId(mirror, out ushort glyphId))
            {
                collection.Replace(i, glyphId, KnownFeatureTags.RightToLeftMirroredForms);
            }
        }

        // TODO: This only replaces certain glyphs. We should investigate the specification further.
        // https://www.unicode.org/reports/tr50/#vertical_alternates
        if (collection.TextOptions.LayoutMode.IsHorizontal())
        {
            return;
        }

        for (int i = 0; i < collection.Count; i++)
        {
            GlyphShapingData data = collection[i];
            if (CodePoint.GetVerticalOrientationType(data.CodePoint) is VerticalOrientationType.Upright or VerticalOrientationType.TransformUpright)
            {
                continue;
            }

            if (!CodePoint.TryGetVerticalMirror(data.CodePoint, out CodePoint mirror))
            {
                continue;
            }

            if (fontMetrics.TryGetGlyphId(mirror, out ushort glyphId))
            {
                collection.Replace(i, glyphId, KnownFeatureTags.VerticalAlternates);
            }
        }
    }

    /// <summary>
    /// Calculates the X offset to apply to a single line of horizontal text so that it is positioned
    /// within the wrapping block according to the requested horizontal and text alignment.
    /// </summary>
    /// <remarks>
    /// The returned offset is in unscaled (pre-Dpi) units and is combined with the pen location at
    /// layout time. The result depends on the text direction because <see cref="TextAlignment.Start"/>
    /// and <see cref="TextAlignment.End"/> flip under right-to-left text.
    /// </remarks>
    /// <param name="lineAdvance">The scaled advance of the current line.</param>
    /// <param name="maxScaledAdvance">The scaled advance of the widest line (or wrapping length, whichever is greater).</param>
    /// <param name="horizontalAlignment">Block-level horizontal alignment of the whole text.</param>
    /// <param name="textAlignment">Per-line alignment within the block.</param>
    /// <param name="direction">The resolved text direction for this line.</param>
    /// <returns>The X offset to add to the line's pen location.</returns>
    internal static float CalculateLineOffsetX(
        float lineAdvance,
        float maxScaledAdvance,
        HorizontalAlignment horizontalAlignment,
        TextAlignment textAlignment,
        TextDirection direction)
    {
        float offsetX = 0;

        // Set the X-Origin for horizontal alignment.
        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Right:
                offsetX = -maxScaledAdvance;
                break;
            case HorizontalAlignment.Center:
                offsetX = -(maxScaledAdvance * .5F);
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (textAlignment)
            {
                case TextAlignment.End:
                    offsetX += maxScaledAdvance - lineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (lineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (textAlignment)
            {
                case TextAlignment.Start:
                    offsetX += maxScaledAdvance - lineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetX += (maxScaledAdvance * .5F) - (lineAdvance * .5F);
                    break;
            }
        }

        return offsetX;
    }

    /// <summary>
    /// Calculates the Y offset to apply to a single line of vertical text so that it is positioned
    /// within the wrapping block according to the requested vertical and text alignment.
    /// </summary>
    /// <remarks>
    /// The returned offset is in unscaled (pre-Dpi) units and is combined with the pen location at
    /// layout time. The result depends on the text direction because <see cref="TextAlignment.Start"/>
    /// and <see cref="TextAlignment.End"/> flip under right-to-left text.
    /// </remarks>
    /// <param name="lineAdvance">The scaled advance of the current line.</param>
    /// <param name="maxScaledAdvance">The scaled advance of the longest line (or wrapping length, whichever is greater).</param>
    /// <param name="verticalAlignment">Block-level vertical alignment of the whole text.</param>
    /// <param name="textAlignment">Per-line alignment within the block.</param>
    /// <param name="direction">The resolved text direction for this line.</param>
    /// <returns>The Y offset to add to the line's pen location.</returns>
    internal static float CalculateLineOffsetY(
        float lineAdvance,
        float maxScaledAdvance,
        VerticalAlignment verticalAlignment,
        TextAlignment textAlignment,
        TextDirection direction)
    {
        float offsetY = 0;

        // Set the Y-Origin for the line.
        switch (verticalAlignment)
        {
            case VerticalAlignment.Top:
                offsetY = 0;
                break;
            case VerticalAlignment.Center:
                offsetY -= maxScaledAdvance * .5F;
                break;
            case VerticalAlignment.Bottom:
                offsetY -= maxScaledAdvance;
                break;
        }

        // Set the alignment of lines within the text.
        if (direction == TextDirection.LeftToRight)
        {
            switch (textAlignment)
            {
                case TextAlignment.End:
                    offsetY += maxScaledAdvance - lineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (lineAdvance * .5F);
                    break;
            }
        }
        else
        {
            switch (textAlignment)
            {
                case TextAlignment.Start:
                    offsetY += maxScaledAdvance - lineAdvance;
                    break;
                case TextAlignment.Center:
                    offsetY += (maxScaledAdvance * .5F) - (lineAdvance * .5F);
                    break;
            }
        }

        return offsetY;
    }
}
