// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests;

public class TextBlockTests
{
    private static readonly ApproximateFloatComparer Comparer = new(0.001F);

    private static Font Font => TextLayoutTests.CreateRenderingFont();

    [Fact]
    public void Constructor_IgnoresWrappingLength()
    {
        const string text = "The quick brown fox jumps over the lazy dog.";
        TextOptions preparedOptions = Options(35);
        TextBlock block = new(text, preparedOptions);

        TextMetrics expected = TextMeasurer.Measure(text, Options(-1));
        TextMetrics actual = block.Measure(-1);

        AssertTextMetricsEqual(expected, actual);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(160)]
    [InlineData(-1)]
    public void Measure_ReusesPreparedText_ForDifferentWrappingLengths(float wrappingLength)
    {
        const string text = "The paragraph begins in English, then says שלום and مرحبا before returning to Latin text.";
        TextBlock block = new(text, Options(-1));

        TextMetrics expected = TextMeasurer.Measure(text, Options(wrappingLength));
        TextMetrics actual = block.Measure(wrappingLength);

        AssertTextMetricsEqual(expected, actual);
    }

    [Fact]
    public void GranularMeasurements_MatchTextMeasurer()
    {
        const string text = "Hello world\nSecond line";
        const float wrappingLength = 95;
        TextBlock block = new(text, Options(-1));

        Assert.Equal(
            TextMeasurer.MeasureAdvance(text, Options(wrappingLength)),
            block.MeasureAdvance(wrappingLength),
            Comparer);

        Assert.Equal(
            TextMeasurer.MeasureBounds(text, Options(wrappingLength)),
            block.MeasureBounds(wrappingLength),
            Comparer);

        Assert.Equal(
            TextMeasurer.MeasureRenderableBounds(text, Options(wrappingLength)),
            block.MeasureRenderableBounds(wrappingLength),
            Comparer);

        Assert.Equal(TextMeasurer.CountLines(text, Options(wrappingLength)), block.CountLines(wrappingLength));
        AssertLineMetricsEqual(TextMeasurer.GetLineMetrics(text, Options(wrappingLength)).Span, block.GetLineMetrics(wrappingLength).Span);
    }

    [Fact]
    public void GetLineMetrics_IncludesSourceMapping()
    {
        const string firstLine = "Hello world\n";
        const string text = firstLine + "Second line";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineMetrics> metrics = block.Measure(-1).LineMetrics;

        Assert.Equal(2, metrics.Length);
        Assert.Equal(0, metrics[0].StringIndex);
        Assert.Equal(firstLine.Length, metrics[1].StringIndex);
        Assert.Equal(0, metrics[0].GraphemeIndex);
        Assert.Equal(metrics[0].GraphemeCount, metrics[1].GraphemeIndex);
    }

    [Fact]
    public void LayoutLines_ReturnsMetricsAndGraphemeMetricsPerLine()
    {
        const string firstLine = "Hello world\n";
        const string text = firstLine + "Second line";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1).Span;
        ReadOnlySpan<LineMetrics> metrics = block.GetLineMetrics(-1).Span;

        Assert.Equal(2, lines.Length);
        AssertLineMetricsEqual(metrics, lines);

        Assert.Equal(firstLine.Length, lines[0].GraphemeMetrics.Length);
        Assert.Equal("Second line".Length, lines[1].GraphemeMetrics.Length);
        Assert.Equal(0, lines[0].GraphemeMetrics[0].StringIndex);
        Assert.Equal(firstLine.Length - 1, lines[0].GraphemeMetrics[^1].StringIndex);
        Assert.Equal(firstLine.Length, lines[1].GraphemeMetrics[0].StringIndex);
    }

    [Fact]
    public void LayoutLines_LeadingHardBreak_IncludesHardBreakGrapheme()
    {
        const string text = "\n\tHelloworld";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1).Span;

        Assert.Equal(2, lines.Length);
        Assert.Equal(1, lines[0].GraphemeMetrics.Length);
        Assert.Equal(0, lines[0].GraphemeMetrics[0].StringIndex);
        Assert.False(lines[1].GraphemeMetrics.IsEmpty);
        Assert.Equal(1, lines[1].GraphemeMetrics[0].StringIndex);
    }

    [Fact]
    public void CharacterMeasurements_MatchTextMeasurer()
    {
        const string text = "A quick test.";
        const float wrappingLength = 70;
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<GlyphBounds> expectedAdvances = TextMeasurer.MeasureGlyphAdvances(text, Options(wrappingLength)).Span;
        ReadOnlySpan<GlyphBounds> actualAdvances = block.MeasureGlyphAdvances(wrappingLength).Span;

        AssertGlyphBoundsEqual(expectedAdvances, actualAdvances);

        ReadOnlySpan<GlyphBounds> expectedBounds = TextMeasurer.MeasureGlyphBounds(text, Options(wrappingLength)).Span;
        ReadOnlySpan<GlyphBounds> actualBounds = block.MeasureGlyphBounds(wrappingLength).Span;

        AssertGlyphBoundsEqual(expectedBounds, actualBounds);

        ReadOnlySpan<GlyphBounds> expectedRenderableBounds = TextMeasurer.MeasureGlyphRenderableBounds(text, Options(wrappingLength)).Span;
        ReadOnlySpan<GlyphBounds> actualRenderableBounds = block.MeasureGlyphRenderableBounds(wrappingLength).Span;

        AssertGlyphBoundsEqual(expectedRenderableBounds, actualRenderableBounds);

        ReadOnlySpan<GraphemeMetrics> expectedGraphemeMetrics = TextMeasurer.GetGraphemeMetrics(text, Options(wrappingLength)).Span;
        ReadOnlySpan<GraphemeMetrics> actualGraphemeMetrics = block.GetGraphemeMetrics(wrappingLength).Span;

        AssertGraphemeMetricsEqual(expectedGraphemeMetrics, actualGraphemeMetrics);
    }

    [Fact]
    public void LayoutLines_MeasureGlyphBounds_MatchBlockSlices()
    {
        const string text = "Hello\nWorld";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1).Span;
        ReadOnlySpan<GlyphBounds> expectedAdvances = block.MeasureGlyphAdvances(-1).Span;
        ReadOnlySpan<GlyphBounds> expectedBounds = block.MeasureGlyphBounds(-1).Span;
        ReadOnlySpan<GlyphBounds> expectedRenderableBounds = block.MeasureGlyphRenderableBounds(-1).Span;

        int glyphIndex = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            ReadOnlySpan<GlyphBounds> lineAdvances = lines[i].MeasureGlyphAdvances().Span;
            ReadOnlySpan<GlyphBounds> lineBounds = lines[i].MeasureGlyphBounds().Span;
            ReadOnlySpan<GlyphBounds> lineRenderableBounds = lines[i].MeasureGlyphRenderableBounds().Span;

            AssertGlyphBoundsEqual(expectedAdvances.Slice(glyphIndex, lineAdvances.Length), lineAdvances);
            AssertGlyphBoundsEqual(expectedBounds.Slice(glyphIndex, lineBounds.Length), lineBounds);
            AssertGlyphBoundsEqual(expectedRenderableBounds.Slice(glyphIndex, lineRenderableBounds.Length), lineRenderableBounds);

            glyphIndex += lineAdvances.Length;
        }

        Assert.Equal(expectedAdvances.Length, glyphIndex);
    }

    [Fact]
    public void HitTest_UsesGraphemeAdvanceMidpoint()
    {
        const string text = "Hi";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        GraphemeMetrics grapheme = metrics.GraphemeMetrics[0];
        Vector2 leadingPoint = new(grapheme.Advance.Left, grapheme.Advance.Top);
        Vector2 trailingPoint = new(grapheme.Advance.Left + (grapheme.Advance.Width * 0.75F), grapheme.Advance.Top);

        TextHit leading = metrics.HitTest(leadingPoint);
        TextHit trailing = metrics.HitTest(trailingPoint);

        Assert.Equal(0, leading.LineIndex);
        Assert.Equal(grapheme.GraphemeIndex, leading.GraphemeIndex);
        Assert.Equal(grapheme.StringIndex, leading.StringIndex);
        Assert.False(leading.IsTrailing);
        Assert.Equal(grapheme.GraphemeIndex, leading.GraphemeInsertionIndex);
        Assert.True(trailing.IsTrailing);
        Assert.Equal(grapheme.GraphemeIndex + 1, trailing.GraphemeInsertionIndex);
    }

    [Fact]
    public void GetCaretPosition_UsesGraphemeAdvanceEdges()
    {
        const string text = "H";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        GraphemeMetrics grapheme = metrics.GraphemeMetrics[0];
        LineMetrics line = metrics.LineMetrics[0];

        CaretPosition leading = metrics.GetCaretPosition(0);
        CaretPosition trailing = metrics.GetCaretPosition(1);

        Assert.Equal(new Vector2(grapheme.Advance.Left, line.Start.Y), leading.Start, Comparer);
        Assert.Equal(new Vector2(grapheme.Advance.Left, line.Start.Y + line.Extent.Y), leading.End, Comparer);
        Assert.Equal(new Vector2(grapheme.Advance.Right, line.Start.Y), trailing.Start, Comparer);
        Assert.Equal(new Vector2(grapheme.Advance.Right, line.Start.Y + line.Extent.Y), trailing.End, Comparer);
        Assert.False(leading.HasSecondary);
        Assert.False(trailing.HasSecondary);
    }

    [Fact]
    public void MoveCaret_PreviousAndNext_MoveByGraphemeInsertionIndex()
    {
        const string text = "ABC";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition caret = metrics.GetCaretPosition(1);

        Assert.Equal(0, metrics.MoveCaret(caret, CaretMovement.Previous).GraphemeIndex);
        Assert.Equal(2, metrics.MoveCaret(caret, CaretMovement.Next).GraphemeIndex);
    }

    [Fact]
    public void MoveCaret_StartAndEnd_MovesWithinLineAndText()
    {
        const string text = "Hi\nYo";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition firstLineCaret = metrics.GetCaretPosition(1);
        CaretPosition secondLineCaret = metrics.GetCaretPosition(4);

        // LineEnd stops before the non-measuring hard break that terminates the first line.
        // TextEnd moves to the final source insertion position of the measured text block.
        Assert.Equal(0, metrics.MoveCaret(firstLineCaret, CaretMovement.LineStart).GraphemeIndex);
        Assert.Equal(2, metrics.MoveCaret(firstLineCaret, CaretMovement.LineEnd).GraphemeIndex);
        Assert.Equal(0, metrics.MoveCaret(secondLineCaret, CaretMovement.TextStart).GraphemeIndex);
        Assert.Equal(5, metrics.MoveCaret(secondLineCaret, CaretMovement.TextEnd).GraphemeIndex);
    }

    [Fact]
    public void MoveCaret_LineDown_PreservesPositionAcrossShortLine()
    {
        const string text = "Hello\nA\nHello";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition firstLineEnd = metrics.GetCaretPosition(5);

        // The first LineDown clamps to the end of the short middle line. The second
        // LineDown should still use the original "Hello" end position, not the
        // clamped middle-line position, so it reaches the end of the final line.
        CaretPosition middleLineEnd = metrics.MoveCaret(firstLineEnd, CaretMovement.LineDown);
        CaretPosition finalLineEnd = metrics.MoveCaret(middleLineEnd, CaretMovement.LineDown);

        Assert.Equal(7, middleLineEnd.GraphemeIndex);
        Assert.Equal(13, finalLineEnd.GraphemeIndex);
        Assert.Equal(firstLineEnd.Start.X, finalLineEnd.Start.X, Comparer);
    }

    [Fact]
    public void HitTest_UsesBidiLogicalTrailingSide()
    {
        const string text = "abc אבג def";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        TextMetrics metrics = TextMeasurer.Measure(text, options);
        GraphemeMetrics grapheme = FindGrapheme(metrics.GraphemeMetrics, 4);
        Vector2 center = FontRectangle.Center(grapheme.Advance);

        // Source order is "abc " then the Hebrew run "אבג" then " def".
        // Grapheme index 4 is "א", the first Hebrew grapheme in logical order.
        // Because the Hebrew run is RTL, "א" is painted at the right-hand side of
        // that run: a point near its visual right edge is before the grapheme
        // logically, while a point near its visual left edge is after it.
        Vector2 leadingPoint = new(grapheme.Advance.Right - (grapheme.Advance.Width * 0.25F), center.Y);
        Vector2 trailingPoint = new(grapheme.Advance.Left + (grapheme.Advance.Width * 0.25F), center.Y);

        TextHit leading = metrics.HitTest(leadingPoint);
        TextHit trailing = metrics.HitTest(trailingPoint);

        Assert.Equal(grapheme.GraphemeIndex, leading.GraphemeIndex);
        Assert.False(leading.IsTrailing);
        Assert.Equal(grapheme.GraphemeIndex, leading.GraphemeInsertionIndex);
        Assert.True(trailing.IsTrailing);
        Assert.Equal(grapheme.GraphemeIndex + 1, trailing.GraphemeInsertionIndex);
    }

    [Fact]
    public void GetSelectionBounds_UsesHitInsertionIndexesForBidiDragSelection()
    {
        const string text = "abc אבג def";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        TextMetrics metrics = TextMeasurer.Measure(text, options);
        GraphemeMetrics first = FindGrapheme(metrics.GraphemeMetrics, 0);
        GraphemeMetrics selectedRtl = FindGrapheme(metrics.GraphemeMetrics, 4);
        GraphemeMetrics unselectedRtl = FindGrapheme(metrics.GraphemeMetrics, 5);

        // The drag starts at the leading edge of "a", giving insertion index 0.
        // The focus point is on the trailing side of logical "א". Since "א" is
        // in an RTL run, that trailing side is its visual left side, so callers
        // should still be able to pass the raw hit without applying bidi rules.
        Vector2 anchorPoint = new(first.Advance.Left, FontRectangle.Center(first.Advance).Y);
        Vector2 focusPoint = new(
            selectedRtl.Advance.Left + (selectedRtl.Advance.Width * 0.25F),
            FontRectangle.Center(selectedRtl.Advance).Y);

        TextHit anchor = metrics.HitTest(anchorPoint);
        TextHit focus = metrics.HitTest(focusPoint);

        Assert.Equal(0, anchor.GraphemeInsertionIndex);
        Assert.Equal(5, focus.GraphemeInsertionIndex);

        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus).Span;

        Assert.Equal(2, selection.Length);
        Assert.True(SelectionContains(selection, FontRectangle.Center(selectedRtl.Advance)));
        Assert.False(SelectionContains(selection, FontRectangle.Center(unselectedRtl.Advance)));
    }

    [Fact]
    public void GetSelectionBounds_UsesCaretPositions()
    {
        const string text = "ABC";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition anchor = metrics.GetCaretPosition(0);
        CaretPosition focus = metrics.MoveCaret(anchor, CaretMovement.Next);

        ReadOnlySpan<FontRectangle> expected = metrics.GetSelectionBounds(0, 1).Span;
        ReadOnlySpan<FontRectangle> actual = metrics.GetSelectionBounds(anchor, focus).Span;

        Assert.Single(actual.ToArray());
        Assert.Equal(expected[0], actual[0], Comparer);
    }

    [Fact]
    public void GetCaretPosition_ExposesSecondaryCaretAtBidiBoundary()
    {
        const string text = "abc אבג def";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        TextMetrics metrics = TextMeasurer.Measure(text, options);
        LineMetrics line = metrics.LineMetrics[0];
        GraphemeMetrics previous = FindGrapheme(metrics.GraphemeMetrics, 3);
        GraphemeMetrics next = FindGrapheme(metrics.GraphemeMetrics, 4);

        // Grapheme index 3 is the LTR space before the Hebrew run, and grapheme
        // index 4 is "א", the first Hebrew grapheme in source order. The logical
        // insertion position 4 is therefore both after the LTR space and before
        // the RTL Hebrew run. Those are different visual edges: the normal caret
        // is at the leading edge of "א" (its visual right edge), while the
        // secondary caret is at the trailing edge of the preceding space.
        CaretPosition caret = metrics.GetCaretPosition(4);

        Assert.Equal(4, caret.GraphemeIndex);
        Assert.True(caret.HasSecondary);
        Assert.Equal(new Vector2(next.Advance.Right, line.Start.Y), caret.Start, Comparer);
        Assert.Equal(new Vector2(next.Advance.Right, line.Start.Y + line.Extent.Y), caret.End, Comparer);
        Assert.Equal(new Vector2(previous.Advance.Right, line.Start.Y), caret.SecondaryStart, Comparer);
        Assert.Equal(new Vector2(previous.Advance.Right, line.Start.Y + line.Extent.Y), caret.SecondaryEnd, Comparer);
    }

    [Fact]
    public void GetSelectionBounds_ReturnsOneRectanglePerVisualLineForContinuousSelection()
    {
        const string text = "Hi\nYo";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));

        // The selected range covers all graphemes in both lines. Since each line is
        // visually continuous, each line should produce one selection rectangle
        // spanning the selected grapheme advances while using the full line box height.
        // The hard break ending the first line remains in the source range but does
        // not contribute to measurement, so it should not widen the painted rectangle.
        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(0, metrics.GraphemeMetrics.Length).Span;

        Assert.Equal(metrics.LineMetrics.Length, selection.Length);
        int graphemeOffset = 0;
        for (int i = 0; i < metrics.LineMetrics.Length; i++)
        {
            LineMetrics line = metrics.LineMetrics[i];
            ReadOnlySpan<GraphemeMetrics> lineGraphemes = metrics.GraphemeMetrics.Slice(graphemeOffset, line.GraphemeCount);
            FontRectangle advance = lineGraphemes[0].Advance;
            float start = advance.Left;
            float end = advance.Right;
            for (int j = 1; j < lineGraphemes.Length; j++)
            {
                if (lineGraphemes[j].IsLineBreak && !lineGraphemes[j].ContributesToMeasurement)
                {
                    continue;
                }

                advance = lineGraphemes[j].Advance;
                start = Math.Min(start, advance.Left);
                end = Math.Max(end, advance.Right);
            }

            FontRectangle expected = FontRectangle.FromLTRB(start, line.Start.Y, end, line.Start.Y + line.Extent.Y);
            Assert.Equal(expected, selection[i], Comparer);
            graphemeOffset += line.GraphemeCount;
        }
    }

    [Fact]
    public void GetSelectionBounds_UsesLineMetricsStartForLineBox()
    {
        const string text = "Hi";
        TextOptions options = Options(-1);
        options.Origin = new(10, 20);
        TextMetrics metrics = TextMeasurer.Measure(text, options);
        LineMetrics line = metrics.LineMetrics[0];
        FontRectangle first = metrics.GraphemeMetrics[0].Advance;
        FontRectangle second = metrics.GraphemeMetrics[1].Advance;

        // Selection rectangles use the line box for the cross-axis extent, not the
        // individual grapheme bounds. The origin therefore shifts the rectangle's
        // Y coordinates even though the horizontal range comes from grapheme advances.
        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(0, metrics.GraphemeMetrics.Length).Span;

        FontRectangle expected = FontRectangle.FromLTRB(
            Math.Min(first.Left, second.Left),
            line.Start.Y,
            Math.Max(first.Right, second.Right),
            line.Start.Y + line.Extent.Y);

        Assert.Equal(expected, selection[0], Comparer);
    }

    [Fact]
    public void GetSelectionBounds_SplitsVisuallyDiscontinuousBidiRange()
    {
        const string text = "abc אבג def";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        TextMetrics metrics = TextMeasurer.Measure(text, options);
        GraphemeMetrics selectedBeforeGap = FindGrapheme(metrics.GraphemeMetrics, 5);
        GraphemeMetrics unselectedGap = FindGrapheme(metrics.GraphemeMetrics, 6);

        // Source graphemes 2..5 are selected: "c", the following space, and
        // Hebrew "א" + "ב". The Hebrew run is visually reversed, so unselected
        // "ג" (grapheme index 6) is painted between the selected LTR fragment
        // and the selected Hebrew fragment. The result should therefore be two
        // selection rectangles, and neither may cover the center of "ג".
        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(2, 6).Span;

        Assert.Equal(2, selection.Length);
        Assert.True(SelectionContains(selection, FontRectangle.Center(selectedBeforeGap.Advance)));
        Assert.False(SelectionContains(selection, FontRectangle.Center(unselectedGap.Advance)));
    }

    [Fact]
    public void GetSelectionBounds_IgnoresNonMeasuringHardBreak()
    {
        const string text = "A\nB";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        GraphemeMetrics hardBreak = FindGrapheme(metrics.GraphemeMetrics, 1);

        // The hard break ends the first line, so line finalization marks it as
        // non-measuring. It remains a logical grapheme at index 1, but selecting
        // only that grapheme should not create a painted selection rectangle.
        Assert.True(hardBreak.IsLineBreak);
        Assert.False(hardBreak.ContributesToMeasurement);

        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(1, 2).Span;

        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void GetSelectionBounds_IncludesMeasuringHardBreak()
    {
        const string text = "\nA";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        GraphemeMetrics hardBreak = FindGrapheme(metrics.GraphemeMetrics, 0);

        // A leading hard break is the only grapheme on its line. It contributes
        // to measurement, so selection should expose the same visible line box
        // behavior as other measuring graphemes.
        Assert.True(hardBreak.IsLineBreak);
        Assert.True(hardBreak.ContributesToMeasurement);

        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(0, 1).Span;

        Assert.Equal(1, selection.Length);
        Assert.True(SelectionContains(selection, FontRectangle.Center(hardBreak.Advance)));
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void GetSelectionBounds_UsesOwningLineInReverseLineOrder(LayoutMode layoutMode)
    {
        const string text = "Hi\nYo";
        TextOptions options = Options(-1);
        options.Origin = new(13, 29);
        options.LayoutMode = layoutMode;
        TextBlock block = new(text, options);

        TextMetrics metrics = block.Measure(-1);
        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1).Span;

        for (int i = 0; i < lines.Length; i++)
        {
            GraphemeMetrics grapheme = lines[i].GraphemeMetrics[0];

            // Reverse line-order modes emit grapheme metrics in visual order, but line metrics
            // retain their source line index. Full-text selection must still find the same
            // owning line slice that the line-local API already has.
            ReadOnlySpan<FontRectangle> expected = lines[i].GetSelectionBounds(grapheme.GraphemeIndex, grapheme.GraphemeIndex + 1).Span;
            ReadOnlySpan<FontRectangle> actual = metrics.GetSelectionBounds(grapheme.GraphemeIndex, grapheme.GraphemeIndex + 1).Span;

            Assert.Single(actual.ToArray());
            Assert.Equal(expected[0], actual[0], Comparer);
        }
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalRightLeft)]
    [InlineData(LayoutMode.VerticalMixedLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void GetLineMetrics_StartMatchesPositionedGraphemeAdvances(LayoutMode layoutMode)
    {
        const string text = "Hi\nYo";
        TextOptions options = Options(-1);
        options.Origin = new(13, 29);
        options.LayoutMode = layoutMode;
        TextBlock block = new(text, options);

        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1).Span;

        Assert.Equal(2, lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            LineMetrics line = lines[i].LineMetrics;
            FontRectangle advance = lines[i].GraphemeMetrics[0].Advance;
            if (layoutMode.IsHorizontal())
            {
                Assert.Equal(advance.Top, line.Start.Y, Comparer);
                Assert.Equal(advance.Height, line.Extent.Y, Comparer);
                continue;
            }

            Assert.Equal(advance.Left, line.Start.X, Comparer);
            Assert.Equal(advance.Width, line.Extent.X, Comparer);
        }
    }

    [Fact]
    public void LineLayoutInteractionMethods_MatchTextMetrics()
    {
        const string text = "Hi\nYo";
        TextBlock block = new(text, Options(-1));
        TextMetrics metrics = block.Measure(-1);
        ReadOnlySpan<LineLayout> lines = block.LayoutLines(-1).Span;
        GraphemeMetrics grapheme = lines[1].GraphemeMetrics[0];
        Vector2 point = FontRectangle.Center(grapheme.Advance);

        TextHit lineHit = lines[1].HitTest(point);
        TextHit metricsHit = metrics.HitTest(point);
        CaretPosition lineCaret = lines[1].GetCaretPosition(grapheme.GraphemeIndex);
        CaretPosition metricsCaret = metrics.GetCaretPosition(grapheme.GraphemeIndex);

        Assert.Equal(metricsHit.LineIndex, lineHit.LineIndex);
        Assert.Equal(metricsHit.GraphemeIndex, lineHit.GraphemeIndex);
        Assert.Equal(metricsHit.StringIndex, lineHit.StringIndex);
        Assert.Equal(metricsHit.IsTrailing, lineHit.IsTrailing);
        Assert.Equal(metricsCaret.Start, lineCaret.Start);
        Assert.Equal(metricsCaret.End, lineCaret.End);
        Assert.Equal(metricsCaret.HasSecondary, lineCaret.HasSecondary);
        Assert.Equal(metricsCaret.SecondaryStart, lineCaret.SecondaryStart);
        Assert.Equal(metricsCaret.SecondaryEnd, lineCaret.SecondaryEnd);
        Assert.Equal(metricsCaret.LineNavigationPosition, lineCaret.LineNavigationPosition, Comparer);

        CaretPosition nextMetricsCaret = metrics.MoveCaret(metricsCaret, CaretMovement.Next);
        CaretPosition nextLineCaret = lines[1].MoveCaret(lineCaret, CaretMovement.Next);

        Assert.Equal(nextMetricsCaret.GraphemeIndex, nextLineCaret.GraphemeIndex);
        Assert.Equal(
            metrics.GetSelectionBounds(grapheme.GraphemeIndex, grapheme.GraphemeIndex + 1).Span[0],
            lines[1].GetSelectionBounds(grapheme.GraphemeIndex, grapheme.GraphemeIndex + 1).Span[0],
            Comparer);

        Assert.Equal(
            metrics.GetSelectionBounds(metricsCaret, nextMetricsCaret).Span[0],
            lines[1].GetSelectionBounds(lineCaret, nextLineCaret).Span[0],
            Comparer);
    }

    [Fact]
    public void EmptyText_ReturnsEmptyMeasurements()
    {
        TextBlock block = new(string.Empty, Options(-1));

        TextMetrics metrics = block.Measure(100);

        Assert.Equal(FontRectangle.Empty, metrics.Advance, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.Bounds, Comparer);
        Assert.Equal(FontRectangle.Empty, metrics.RenderableBounds, Comparer);
        Assert.Equal(0, metrics.LineCount);
        Assert.True(metrics.MeasureGlyphAdvances().IsEmpty);
        Assert.True(metrics.MeasureGlyphBounds().IsEmpty);
        Assert.True(metrics.MeasureGlyphRenderableBounds().IsEmpty);
        Assert.True(metrics.GraphemeMetrics.IsEmpty);
        Assert.True(metrics.LineMetrics.IsEmpty);
        Assert.Equal(FontRectangle.Empty, block.MeasureAdvance(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureBounds(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureRenderableBounds(100), Comparer);
        Assert.Equal(0, block.CountLines(100));
        Assert.True(block.GetLineMetrics(100).IsEmpty);

        Assert.True(block.MeasureGlyphAdvances(100).IsEmpty);

        Assert.True(block.MeasureGlyphBounds(100).IsEmpty);

        Assert.True(block.MeasureGlyphRenderableBounds(100).IsEmpty);

        Assert.True(block.GetGraphemeMetrics(100).IsEmpty);
    }

    private static TextOptions Options(float wrappingLength)
        => new(Font) { WrappingLength = wrappingLength };

    private static GraphemeMetrics FindGrapheme(ReadOnlySpan<GraphemeMetrics> graphemes, int graphemeIndex)
    {
        for (int i = 0; i < graphemes.Length; i++)
        {
            if (graphemes[i].GraphemeIndex == graphemeIndex)
            {
                return graphemes[i];
            }
        }

        throw new InvalidOperationException("The expected grapheme was not measured.");
    }

    private static bool SelectionContains(ReadOnlySpan<FontRectangle> selection, Vector2 point)
    {
        for (int i = 0; i < selection.Length; i++)
        {
            if (selection[i].Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertTextMetricsEqual(TextMetrics expected, TextMetrics actual)
    {
        Assert.Equal(expected.Advance, actual.Advance, Comparer);
        Assert.Equal(expected.Bounds, actual.Bounds, Comparer);
        Assert.Equal(expected.RenderableBounds, actual.RenderableBounds, Comparer);
        Assert.Equal(expected.LineCount, actual.LineCount);
        AssertGlyphBoundsEqual(expected.MeasureGlyphAdvances().Span, actual.MeasureGlyphAdvances().Span);
        AssertGlyphBoundsEqual(expected.MeasureGlyphBounds().Span, actual.MeasureGlyphBounds().Span);
        AssertGlyphBoundsEqual(expected.MeasureGlyphRenderableBounds().Span, actual.MeasureGlyphRenderableBounds().Span);
        AssertGraphemeMetricsEqual(expected.GraphemeMetrics, actual.GraphemeMetrics);
        AssertLineMetricsEqual(expected.LineMetrics, actual.LineMetrics);
    }

    private static void AssertGlyphBoundsEqual(ReadOnlySpan<GlyphBounds> expected, ReadOnlySpan<GlyphBounds> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertGlyphBoundsEqual(expected[i], actual[i]);
        }
    }

    private static void AssertGlyphBoundsEqual(GlyphBounds expected, GlyphBounds actual)
    {
        Assert.Equal(expected.Codepoint, actual.Codepoint);
        Assert.Equal(expected.Bounds, actual.Bounds, Comparer);
        Assert.Equal(expected.GraphemeIndex, actual.GraphemeIndex);
        Assert.Equal(expected.StringIndex, actual.StringIndex);
    }

    private static void AssertGraphemeMetricsEqual(ReadOnlySpan<GraphemeMetrics> expected, ReadOnlySpan<GraphemeMetrics> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Advance, actual[i].Advance, Comparer);
            Assert.Equal(expected[i].Bounds, actual[i].Bounds, Comparer);
            Assert.Equal(expected[i].RenderableBounds, actual[i].RenderableBounds, Comparer);
            Assert.Equal(expected[i].GraphemeIndex, actual[i].GraphemeIndex);
            Assert.Equal(expected[i].StringIndex, actual[i].StringIndex);
            Assert.Equal(expected[i].BidiLevel, actual[i].BidiLevel);
            Assert.Equal(expected[i].IsLineBreak, actual[i].IsLineBreak);
            Assert.Equal(expected[i].ContributesToMeasurement, actual[i].ContributesToMeasurement);
        }
    }

    private static void AssertLineMetricsEqual(ReadOnlySpan<LineMetrics> expected, ReadOnlySpan<LineMetrics> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Ascender, actual[i].Ascender, Comparer);
            Assert.Equal(expected[i].Baseline, actual[i].Baseline, Comparer);
            Assert.Equal(expected[i].Descender, actual[i].Descender, Comparer);
            Assert.Equal(expected[i].LineHeight, actual[i].LineHeight, Comparer);
            Assert.Equal(expected[i].Start, actual[i].Start, Comparer);
            Assert.Equal(expected[i].Extent, actual[i].Extent, Comparer);
            Assert.Equal(expected[i].StringIndex, actual[i].StringIndex);
            Assert.Equal(expected[i].GraphemeIndex, actual[i].GraphemeIndex);
            Assert.Equal(expected[i].GraphemeCount, actual[i].GraphemeCount);
        }
    }

    private static void AssertLineMetricsEqual(ReadOnlySpan<LineMetrics> expected, ReadOnlySpan<LineLayout> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Ascender, actual[i].LineMetrics.Ascender, Comparer);
            Assert.Equal(expected[i].Baseline, actual[i].LineMetrics.Baseline, Comparer);
            Assert.Equal(expected[i].Descender, actual[i].LineMetrics.Descender, Comparer);
            Assert.Equal(expected[i].LineHeight, actual[i].LineMetrics.LineHeight, Comparer);
            Assert.Equal(expected[i].Start, actual[i].LineMetrics.Start, Comparer);
            Assert.Equal(expected[i].Extent, actual[i].LineMetrics.Extent, Comparer);
            Assert.Equal(expected[i].StringIndex, actual[i].LineMetrics.StringIndex);
            Assert.Equal(expected[i].GraphemeIndex, actual[i].LineMetrics.GraphemeIndex);
            Assert.Equal(expected[i].GraphemeCount, actual[i].LineMetrics.GraphemeCount);
        }
    }
}
