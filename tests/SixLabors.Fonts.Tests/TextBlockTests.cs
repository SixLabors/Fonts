// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

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

        // The newline that ends the first line is trimmed from visual metrics, so
        // the next line starts after a source-position gap.
        Assert.Equal(firstLine.Length, metrics[1].GraphemeIndex);
    }

    [Fact]
    public void GetLineLayouts_ReturnsMetricsAndGraphemeMetricsPerLine()
    {
        const string firstLine = "Hello world\n";
        const string text = firstLine + "Second line";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.GetLineLayouts(-1).Span;
        ReadOnlySpan<LineMetrics> metrics = block.GetLineMetrics(-1).Span;

        Assert.Equal(2, lines.Length);
        AssertLineMetricsEqual(metrics, lines);

        Assert.Equal(firstLine.Length - 1, lines[0].GraphemeMetrics.Length);
        Assert.Equal("Second line".Length, lines[1].GraphemeMetrics.Length);
        Assert.Equal(0, lines[0].GraphemeMetrics[0].StringIndex);
        Assert.Equal(firstLine.Length - 2, lines[0].GraphemeMetrics[^1].StringIndex);
        Assert.Equal(firstLine.Length, lines[1].GraphemeMetrics[0].StringIndex);
    }

    [Fact]
    public void GetLineLayouts_LeadingHardBreak_IncludesHardBreakGrapheme()
    {
        const string text = "\n\tHelloworld";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.GetLineLayouts(-1).Span;

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
    public void TextHyphenation_None_IgnoresSoftHyphenBreak()
    {
        const string text = "extra\u00ADordinary";
        TextOptions measureOptions = Options(-1);
        float markerBreakAdvance = TextMeasurer.MeasureAdvance("extra-", measureOptions).Width;
        float fullAdvance = TextMeasurer.MeasureAdvance("extraordinary", measureOptions).Width;

        TextOptions options = Options(markerBreakAdvance + ((fullAdvance - markerBreakAdvance) * 0.5F));
        options.TextHyphenation = TextHyphenation.None;

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The wrapping width is deliberately narrow enough that "extra-" would fit and the
        // full word would not. With hyphenation disabled, U+00AD remains source-mapping data
        // only and must not become a line break or a visible marker.
        Assert.Equal(1, metrics.LineCount);
        Assert.Equal(0, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint('-')));
    }

    [Fact]
    public void TextHyphenation_Custom_InsertsMarkerWhenSoftHyphenBreakIsSelected()
    {
        const string text = "extra\u00ADordinary";
        TextOptions measureOptions = Options(-1);
        float markerBreakAdvance = TextMeasurer.MeasureAdvance("extra*", measureOptions).Width;
        float fullAdvance = TextMeasurer.MeasureAdvance("extraordinary", measureOptions).Width;

        TextOptions options = Options(markerBreakAdvance + ((fullAdvance - markerBreakAdvance) * 0.5F));
        options.TextHyphenation = TextHyphenation.Custom;
        options.CustomHyphen = new('*');

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        GraphemeMetrics softHyphen = FindGrapheme(metrics.GraphemeMetrics, 5);

        // The source soft hyphen is a real grapheme at index 5, but it is only rendered
        // when its discretionary break is selected. The generated marker uses the caller's
        // custom codepoint while keeping that same source mapping, so selection and caret
        // APIs still see one grapheme.
        Assert.Equal(2, metrics.LineCount);
        Assert.Equal(1, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint('*')));
        Assert.Equal(0, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint('-')));
        Assert.Equal(5, softHyphen.StringIndex);
    }

    [Fact]
    public void TextHyphenation_Custom_DoesNotInsertMarkerBeforeHardBreak()
    {
        const string text = "extra\u00AD\nordinary";
        TextOptions options = Options(1000);
        options.TextHyphenation = TextHyphenation.Custom;
        options.CustomHyphen = new('*');

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The required newline is the selected break. A preceding U+00AD that was not
        // selected must stay invisible; otherwise hard-break layout would grow a marker
        // that the source text did not ask to display at that point.
        Assert.Equal(2, metrics.LineCount);
        Assert.Equal(0, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint('*')));
    }

    [Fact]
    public void TextHyphenation_Custom_AccountsForMarkerAdvanceWhenChoosingBreak()
    {
        const string text = "a extra\u00ADordinary";
        TextOptions measureOptions = Options(-1);
        float softBreakWithoutMarker = TextMeasurer.MeasureAdvance("a extra", measureOptions).Width;
        float softBreakWithMarker = TextMeasurer.MeasureAdvance("a extra*", measureOptions).Width;

        TextOptions options = Options(softBreakWithoutMarker + ((softBreakWithMarker - softBreakWithoutMarker) * 0.5F));
        options.TextHyphenation = TextHyphenation.Custom;
        options.CustomHyphen = new('*');

        TextBlock block = new(text, options);
        ReadOnlySpan<LineLayout> lines = block.GetLineLayouts(options.WrappingLength).Span;

        // Without the generated marker advance, the soft-hyphen break after "extra"
        // would appear to fit. Including that advance keeps the earlier space break,
        // so the second line starts after "a " rather than after the soft hyphen.
        Assert.True(lines.Length >= 2);
        Assert.Equal(0, lines[0].LineMetrics.StringIndex);
        Assert.Equal(2, lines[1].LineMetrics.StringIndex);
    }

    [Fact]
    public void TextEllipsis_Standard_InsertsMarkerWhenMaxLinesHidesText()
    {
        const string text = "one two three four five";
        TextOptions options = Options(TextMeasurer.MeasureAdvance("one two", Options(-1)).Width);
        options.MaxLines = 1;
        options.TextEllipsis = TextEllipsis.Standard;

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The source would wrap onto later lines, but MaxLines exposes only the
        // first visual line. Standard ellipsis should replace the tail of that
        // final visible line with one U+2026 marker.
        Assert.Equal(1, metrics.LineCount);
        Assert.Equal(1, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint(0x2026)));
    }

    [Fact]
    public void TextEllipsis_Custom_InsertsConfiguredMarkerWhenMaxLinesHidesText()
    {
        const string text = "one two three four five";
        TextOptions options = Options(TextMeasurer.MeasureAdvance("one two", Options(-1)).Width);
        options.MaxLines = 1;
        options.TextEllipsis = TextEllipsis.Custom;
        options.CustomEllipsis = new('*');

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // Custom ellipsis uses the supplied codepoint instead of the standard
        // marker while preserving the same max-lines truncation behavior.
        Assert.Equal(1, metrics.LineCount);
        Assert.Equal(1, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint('*')));
        Assert.Equal(0, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint(0x2026)));
    }

    [Fact]
    public void TextEllipsis_None_LimitsLinesWithoutMarker()
    {
        const string text = "one two three four five";
        TextOptions options = Options(TextMeasurer.MeasureAdvance("one two", Options(-1)).Width);
        options.MaxLines = 1;
        options.TextEllipsis = TextEllipsis.None;

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // MaxLines still hides later lines when ellipsis is disabled; the final
        // visible line is simply clipped at the selected line boundary with no
        // generated marker.
        Assert.Equal(1, metrics.LineCount);
        Assert.Equal(0, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint(0x2026)));
    }

    [Fact]
    public void TextEllipsis_Standard_DoesNotInsertMarkerWhenTextFitsMaxLines()
    {
        const string text = "one two";
        TextOptions options = Options(1000);
        options.MaxLines = 2;
        options.TextEllipsis = TextEllipsis.Standard;

        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // The marker is only generated when MaxLines actually hides source text.
        // A fitting paragraph keeps its original glyph stream unchanged.
        Assert.Equal(1, metrics.LineCount);
        Assert.Equal(0, CountGlyphs(metrics.MeasureGlyphAdvances().Span, new CodePoint(0x2026)));
    }

    [Fact]
    public void TextBidiMode_Normal_KeepsLatinRunsInSourceOrder()
    {
        const string text = "abc def";
        TextOptions options = Options(-1);
        options.TextDirection = TextDirection.RightToLeft;
        options.TextBidiMode = TextBidiMode.Normal;

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        ReadOnlySpan<GlyphBounds> glyphs = metrics.MeasureGlyphAdvances().Span;

        // Normal bidi uses RTL as the paragraph direction, but Latin remains a
        // strong LTR run. The run may be right-aligned by layout, but its glyph
        // order is still the readable source order.
        Assert.Equal(new CodePoint('a'), glyphs[0].Codepoint);
        Assert.Equal(new CodePoint('b'), glyphs[1].Codepoint);
        Assert.Equal(new CodePoint('c'), glyphs[2].Codepoint);
    }

    [Fact]
    public void TextBidiMode_Override_ReversesLatinRunsInResolvedDirection()
    {
        const string text = "abc def";
        TextOptions options = Options(-1);
        options.TextDirection = TextDirection.RightToLeft;
        options.TextBidiMode = TextBidiMode.Override;

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        ReadOnlySpan<GlyphBounds> glyphs = metrics.MeasureGlyphAdvances().Span;

        // Override feeds the bidi algorithm with the requested strong direction
        // for real text. Under RTL override, Latin no longer forms an LTR run,
        // so the final visual glyph order is reversed.
        Assert.Equal(new CodePoint('f'), glyphs[0].Codepoint);
        Assert.Equal(new CodePoint('e'), glyphs[1].Codepoint);
        Assert.Equal(new CodePoint('d'), glyphs[2].Codepoint);
    }

    [Fact]
    public void TextBidiMode_Normal_KeepsHebrewRunsInResolvedRightToLeftOrder()
    {
        const string text = "אבג דהו";
        TextOptions options = new(TextLayoutTests.CreateFont(text))
        {
            TextDirection = TextDirection.LeftToRight,
            TextBidiMode = TextBidiMode.Normal
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        ReadOnlySpan<GlyphBounds> glyphs = metrics.MeasureGlyphAdvances().Span;

        // Normal bidi keeps Hebrew as a strong RTL run inside the LTR paragraph.
        // The first visual glyph is therefore the final Hebrew grapheme in the
        // source phrase, not the first source grapheme.
        Assert.Equal(new CodePoint('ו'), glyphs[0].Codepoint);
        Assert.Equal(new CodePoint('ה'), glyphs[1].Codepoint);
        Assert.Equal(new CodePoint('ד'), glyphs[2].Codepoint);
    }

    [Fact]
    public void TextBidiMode_Override_ReversesHebrewRunsInResolvedDirection()
    {
        const string text = "אבג דהו";
        TextOptions options = new(TextLayoutTests.CreateFont(text))
        {
            TextDirection = TextDirection.LeftToRight,
            TextBidiMode = TextBidiMode.Override
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        ReadOnlySpan<GlyphBounds> glyphs = metrics.MeasureGlyphAdvances().Span;

        // LTR override forces the Hebrew letters through LTR bidi resolution,
        // visibly reversing the normal RTL run order.
        Assert.Equal(new CodePoint('א'), glyphs[0].Codepoint);
        Assert.Equal(new CodePoint('ב'), glyphs[1].Codepoint);
        Assert.Equal(new CodePoint('ג'), glyphs[2].Codepoint);
    }

    [Fact]
    public void GetLineLayouts_MeasureGlyphBounds_MatchBlockSlices()
    {
        const string text = "Hello\nWorld";
        TextBlock block = new(text, Options(-1));

        ReadOnlySpan<LineLayout> lines = block.GetLineLayouts(-1).Span;
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

        CaretPosition leading = metrics.GetCaret(CaretPlacement.Start);
        CaretPosition trailing = metrics.GetCaret(CaretPlacement.End);

        Assert.Equal(new Vector2(grapheme.Advance.Left, line.Start.Y), leading.Start, Comparer);
        Assert.Equal(new Vector2(grapheme.Advance.Left, line.Start.Y + line.Extent.Y), leading.End, Comparer);
        Assert.Equal(new Vector2(grapheme.Advance.Right, line.Start.Y), trailing.Start, Comparer);
        Assert.Equal(new Vector2(grapheme.Advance.Right, line.Start.Y + line.Extent.Y), trailing.End, Comparer);
        Assert.False(leading.HasSecondary);
        Assert.False(trailing.HasSecondary);
    }

    [Fact]
    public void GetCaretPosition_UsesResolvedDirectionForRtlStartAndEnd()
    {
        const string text = "אבג";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            TextDirection = TextDirection.RightToLeft
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        LineMetrics line = metrics.LineMetrics[0];

        // In RTL text, the source start maps to the physical end of the line box,
        // and the source end maps to the physical start of that same line box.
        CaretPosition start = metrics.GetCaret(CaretPlacement.Start);
        CaretPosition end = metrics.GetCaret(CaretPlacement.End);

        float startX = line.Start.X + line.Extent.X;
        float endX = line.Start.X;

        Assert.Equal(new Vector2(startX, line.Start.Y), start.Start, Comparer);
        Assert.Equal(new Vector2(startX, line.Start.Y + line.Extent.Y), start.End, Comparer);
        Assert.Equal(new Vector2(endX, line.Start.Y), end.Start, Comparer);
        Assert.Equal(new Vector2(endX, line.Start.Y + line.Extent.Y), end.End, Comparer);
    }

    [Fact]
    public void MoveCaret_PreviousAndNext_MoveByGraphemeInsertionIndex()
    {
        const string text = "ABC";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition caret = metrics.MoveCaret(metrics.GetCaret(CaretPlacement.Start), CaretMovement.Next);

        Assert.Equal(0, metrics.MoveCaret(caret, CaretMovement.Previous).GraphemeIndex);
        Assert.Equal(2, metrics.MoveCaret(caret, CaretMovement.Next).GraphemeIndex);
    }

    [Fact]
    public void MoveCaret_PreviousAndNext_UseSourceOrderThroughBidiText()
    {
        const string text = "abc אבג";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font) { Dpi = font.FontMetrics.ScaleFactor };
        TextMetrics metrics = TextMeasurer.Measure(text, options);

        // Previous/Next model keyboard movement through the source string. Inside
        // a bidi run that means the caret may jump visually, but the source
        // insertion indices still advance one grapheme boundary at a time.
        CaretPosition caret = metrics.GetCaret(CaretPlacement.Start);
        for (int i = 0; i < 5; i++)
        {
            caret = metrics.MoveCaret(caret, CaretMovement.Next);
        }

        Assert.Equal(5, caret.GraphemeIndex);
        Assert.Equal(4, metrics.MoveCaret(caret, CaretMovement.Previous).GraphemeIndex);
        Assert.Equal(6, metrics.MoveCaret(caret, CaretMovement.Next).GraphemeIndex);
    }

    [Fact]
    public void GetWordMetrics_UsesUnicodeWordBoundarySegments()
    {
        const string text = "can't stop";
        TextBlock block = new(text, Options(-1));
        TextMetrics metrics = block.Measure(-1);

        ReadOnlySpan<WordMetrics> blockMetrics = block.GetWordMetrics(-1).Span;
        ReadOnlySpan<WordMetrics> metricWords = metrics.WordMetrics;

        // UAX #29 keeps the apostrophe inside "can't", but the space remains its own
        // word-boundary segment. That matters because browser-style double-click and
        // word navigation can land on separator segments as well as letter segments.
        Assert.Equal(3, blockMetrics.Length);
        AssertWordMetrics(blockMetrics[0], 0, 5, 0, 5);
        AssertWordMetrics(blockMetrics[1], 5, 6, 5, 6);
        AssertWordMetrics(blockMetrics[2], 6, 10, 6, 10);

        // TextBlock.GetWordMetrics uses the direct word-only path, while Measure exposes
        // the word metrics gathered alongside full grapheme metrics. The public rectangles
        // and source ranges must match so callers can choose either entry point freely.
        AssertWordMetrics(blockMetrics[0], metricWords[0]);
        AssertWordMetrics(blockMetrics[1], metricWords[1]);
        AssertWordMetrics(blockMetrics[2], metricWords[2]);

        // Whitespace is a word-boundary segment and it has measurable advance/bounds in
        // this API, so the space word metrics should be exactly the space grapheme metrics.
        GraphemeMetrics space = FindGrapheme(metrics.GraphemeMetrics, 5);
        Assert.Equal(space.Advance, metricWords[1].Advance, Comparer);
        Assert.Equal(space.Bounds, metricWords[1].Bounds, Comparer);
        Assert.Equal(space.RenderableBounds, metricWords[1].RenderableBounds, Comparer);
    }

    [Theory]
    [InlineData(LayoutMode.HorizontalTopBottom)]
    [InlineData(LayoutMode.HorizontalBottomTop)]
    [InlineData(LayoutMode.VerticalLeftRight)]
    [InlineData(LayoutMode.VerticalMixedRightLeft)]
    public void GetWordMetrics_MatchesMeasureWordMetrics_ForComplexLayout(LayoutMode layoutMode)
    {
        const string text = "can't e\u0301 שלום\nwrap אבג stop";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            LayoutMode = layoutMode,
            WrappingLength = 110
        };

        TextBlock block = new(text, options);

        // Measure builds grapheme metrics and word metrics in the same visitor. GetWordMetrics
        // uses the allocation-saving word-only visitor, so this text deliberately mixes word
        // separators, a multi-codepoint grapheme, bidi runs, a hard break, and wrapping to pin
        // every flush path against the full measurement pipeline.
        ReadOnlySpan<WordMetrics> expected = block.Measure(options.WrappingLength).WordMetrics;
        ReadOnlySpan<WordMetrics> actual = block.GetWordMetrics(options.WrappingLength).Span;

        AssertWordMetricsEqual(expected, actual);
    }

    [Fact]
    public void GetWordMetrics_UsesHitGraphemeForTrailingSide()
    {
        const string text = "can't stop";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        GraphemeMetrics finalWordGrapheme = FindGrapheme(metrics.GraphemeMetrics, 4);
        Vector2 trailingPoint = new(
            finalWordGrapheme.Advance.Right - (finalWordGrapheme.Advance.Width * 0.25F),
            FontRectangle.Center(finalWordGrapheme.Advance).Y);

        TextHit hit = metrics.HitTest(trailingPoint);
        WordMetrics word = metrics.GetWordMetrics(hit);

        // The hit is on the trailing side of "t", so its insertion index is after the word.
        // Word selection still uses the hit grapheme itself, not the following space segment.
        Assert.Equal(5, hit.GraphemeInsertionIndex);
        AssertWordMetrics(word, 0, 5, 0, 5);
    }

    [Fact]
    public void MoveCaret_PreviousWordAndNextWord_MoveByUnicodeWordBoundaries()
    {
        const string text = "can't stop";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));

        // UAX #29 word boundaries produce three segments here: "can't", " ", and "stop".
        // Word movement walks those boundaries in source order rather than skipping separators.
        CaretPosition caret = metrics.GetCaret(CaretPlacement.Start);
        caret = metrics.MoveCaret(caret, CaretMovement.NextWord);
        Assert.Equal(5, caret.GraphemeIndex);

        caret = metrics.MoveCaret(caret, CaretMovement.NextWord);
        Assert.Equal(6, caret.GraphemeIndex);

        caret = metrics.MoveCaret(caret, CaretMovement.NextWord);
        Assert.Equal(10, caret.GraphemeIndex);

        caret = metrics.MoveCaret(caret, CaretMovement.PreviousWord);
        Assert.Equal(6, caret.GraphemeIndex);

        caret = metrics.MoveCaret(caret, CaretMovement.PreviousWord);
        Assert.Equal(5, caret.GraphemeIndex);

        caret = metrics.MoveCaret(caret, CaretMovement.PreviousWord);
        Assert.Equal(0, caret.GraphemeIndex);
    }

    [Fact]
    public void GetSelectionBounds_UsesWordMetrics()
    {
        const string text = "can't stop";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        GraphemeMetrics wordGrapheme = FindGrapheme(metrics.GraphemeMetrics, 2);
        TextHit hit = metrics.HitTest(FontRectangle.Center(wordGrapheme.Advance));
        WordMetrics word = metrics.GetWordMetrics(hit);

        ReadOnlySpan<FontRectangle> actual = metrics.GetSelectionBounds(word).Span;

        Assert.Equal(1, actual.Length);
        AssertWordMetrics(word, 0, 5, 0, 5);
        Assert.True(SelectionContains(actual, FontRectangle.Center(wordGrapheme.Advance)));
    }

    [Fact]
    public void GetSelectionBounds_UsesCaretPositionsMovedByWord()
    {
        const string text = "can't stop";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition anchor = metrics.GetCaret(CaretPlacement.Start);

        // This mimics Shift+Ctrl+Right on Windows-style editors: the anchor stays where
        // selection began, while the focus caret advances by Unicode word-boundary segments.
        // The first move selects "can't"; the second also selects the separator segment
        // because our word movement intentionally exposes spaces as selectable segments.
        CaretPosition focus = metrics.MoveCaret(anchor, CaretMovement.NextWord);
        AssertSelectionBoundsEqual(metrics.GetSelectionBounds(metrics.GetWordMetrics(anchor)).Span, metrics.GetSelectionBounds(anchor, focus).Span);

        focus = metrics.MoveCaret(focus, CaretMovement.NextWord);
        ReadOnlyMemory<FontRectangle> firstTwoWords = metrics.GetSelectionBounds(anchor, focus);

        Assert.True(SelectionContains(firstTwoWords.Span, FontRectangle.Center(FindGrapheme(metrics.GraphemeMetrics, 0).Advance)));
        Assert.True(SelectionContains(firstTwoWords.Span, FontRectangle.Center(FindGrapheme(metrics.GraphemeMetrics, 5).Advance)));

        // Reverse word selection should produce the same rectangles for the same insertion
        // range even though the caret movement started at the far end of the text.
        CaretPosition reverseAnchor = metrics.GetCaret(CaretPlacement.End);
        CaretPosition reverseFocus = metrics.MoveCaret(reverseAnchor, CaretMovement.PreviousWord);

        AssertSelectionBoundsEqual(metrics.GetSelectionBounds(metrics.GetWordMetrics(reverseFocus)).Span, metrics.GetSelectionBounds(reverseAnchor, reverseFocus).Span);
    }

    [Fact]
    public void MoveCaret_StartAndEnd_MovesWithinLineAndText()
    {
        const string text = "Hi\nYo";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition firstLineCaret = metrics.MoveCaret(metrics.GetCaret(CaretPlacement.Start), CaretMovement.Next);
        CaretPosition secondLineCaret = metrics.MoveCaret(metrics.GetCaret(CaretPlacement.End), CaretMovement.Previous);

        // LineEnd stops at the source gap left by the trimmed hard break.
        // TextEnd moves to the final source insertion position of the measured text block.
        Assert.Equal(0, metrics.MoveCaret(firstLineCaret, CaretMovement.LineStart).GraphemeIndex);
        Assert.Equal(2, metrics.MoveCaret(firstLineCaret, CaretMovement.LineEnd).GraphemeIndex);
        Assert.Equal(0, metrics.MoveCaret(secondLineCaret, CaretMovement.TextStart).GraphemeIndex);
        Assert.Equal(5, metrics.MoveCaret(secondLineCaret, CaretMovement.TextEnd).GraphemeIndex);
    }

    [Fact]
    public void MoveCaret_LineStartAndLineEnd_UseResolvedDirectionForRtlLine()
    {
        const string text = "abc\nאבג";
        Font font = TextLayoutTests.CreateFont(text);
        TextOptions options = new(font)
        {
            Dpi = font.FontMetrics.ScaleFactor,
            TextDirection = TextDirection.RightToLeft
        };

        TextMetrics metrics = TextMeasurer.Measure(text, options);
        GraphemeMetrics middleHebrew = FindGrapheme(metrics.GraphemeMetrics, 5);
        CaretPosition caret = metrics.GetCaretPosition(metrics.HitTest(FontRectangle.Center(middleHebrew.Advance)));
        LineMetrics line = metrics.LineMetrics[1];

        // Home/End style movement uses the line's source boundaries. For RTL,
        // LineStart maps to the physical end of the line box, and LineEnd maps
        // to the physical start of that same line box.
        CaretPosition lineStart = metrics.MoveCaret(caret, CaretMovement.LineStart);
        CaretPosition lineEnd = metrics.MoveCaret(caret, CaretMovement.LineEnd);

        float lineStartX = line.Start.X + line.Extent.X;
        float lineEndX = line.Start.X;

        Assert.Equal(new Vector2(lineStartX, line.Start.Y), lineStart.Start, Comparer);
        Assert.Equal(new Vector2(lineStartX, line.Start.Y + line.Extent.Y), lineStart.End, Comparer);
        Assert.Equal(new Vector2(lineEndX, line.Start.Y), lineEnd.Start, Comparer);
        Assert.Equal(new Vector2(lineEndX, line.Start.Y + line.Extent.Y), lineEnd.End, Comparer);
    }

    [Fact]
    public void MoveCaret_LineDown_PreservesPositionAcrossShortLine()
    {
        const string text = "Hello\nA\nHello";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition firstLineEnd = metrics.MoveCaret(metrics.GetCaret(CaretPlacement.Start), CaretMovement.LineEnd);

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
    public void MoveCaret_LineUp_PreservesPositionAcrossShortLine()
    {
        const string text = "Hello\nA\nHello";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        CaretPosition finalLineEnd = metrics.MoveCaret(metrics.GetCaret(CaretPlacement.End), CaretMovement.LineEnd);

        // The first LineUp clamps to the end of the short middle line. The second
        // LineUp should still use the original "Hello" end position, not the
        // clamped middle-line position, so it reaches the end of the first line.
        CaretPosition middleLineEnd = metrics.MoveCaret(finalLineEnd, CaretMovement.LineUp);
        CaretPosition firstLineEnd = metrics.MoveCaret(middleLineEnd, CaretMovement.LineUp);

        Assert.Equal(7, middleLineEnd.GraphemeIndex);
        Assert.Equal(5, firstLineEnd.GraphemeIndex);
        Assert.Equal(finalLineEnd.Start.X, firstLineEnd.Start.X, Comparer);
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
        CaretPosition anchor = metrics.GetCaret(CaretPlacement.Start);
        CaretPosition focus = metrics.MoveCaret(anchor, CaretMovement.Next);

        ReadOnlySpan<FontRectangle> actual = metrics.GetSelectionBounds(anchor, focus).Span;

        Assert.Single(actual.ToArray());
        Assert.Equal(metrics.GetSelectionBounds(metrics.GraphemeMetrics[0]).Span[0], actual[0], Comparer);
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
        CaretPosition caret = metrics.GetCaretPosition(metrics.HitTest(FontRectangle.Center(next.Advance)));

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

        // The selected range covers both visual lines. Since each line is visually
        // continuous, each line should produce one selection rectangle spanning
        // the selected grapheme advances while using the full line box height.
        CaretPosition anchor = metrics.GetCaret(CaretPlacement.Start);
        CaretPosition focus = metrics.MoveCaret(anchor, CaretMovement.TextEnd);
        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus).Span;

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
        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(metrics.GetCaret(CaretPlacement.Start), metrics.GetCaret(CaretPlacement.End)).Span;

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
        CaretPosition anchor = metrics.GetCaret(CaretPlacement.Start);
        anchor = metrics.MoveCaret(anchor, CaretMovement.Next);
        anchor = metrics.MoveCaret(anchor, CaretMovement.Next);
        CaretPosition focus = anchor;
        for (int i = 0; i < 4; i++)
        {
            focus = metrics.MoveCaret(focus, CaretMovement.Next);
        }

        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus).Span;

        Assert.Equal(2, selection.Length);
        Assert.True(SelectionContains(selection, FontRectangle.Center(selectedBeforeGap.Advance)));
        Assert.False(SelectionContains(selection, FontRectangle.Center(unselectedGap.Advance)));
    }

    [Fact]
    public void GetSelectionBounds_IgnoresTrimmedHardBreak()
    {
        const string text = "A\nB";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));

        // The hard break ends a non-empty line, so it is trimmed with trailing
        // breaking whitespace. Selecting only that source grapheme therefore
        // has no measured grapheme to paint.
        Assert.DoesNotContain(metrics.GraphemeMetrics.ToArray(), x => x.GraphemeIndex == 1);

        CaretPosition anchor = metrics.MoveCaret(metrics.GetCaret(CaretPlacement.Start), CaretMovement.Next);
        CaretPosition focus = metrics.MoveCaret(anchor, CaretMovement.Next);
        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus).Span;

        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void GetSelectionBounds_IncludesBlankLineHardBreak()
    {
        const string text = "\nA";
        TextMetrics metrics = TextMeasurer.Measure(text, Options(-1));
        GraphemeMetrics hardBreak = FindGrapheme(metrics.GraphemeMetrics, 0);

        // A leading hard break is the only grapheme on its line. It is preserved
        // so the blank line has real line geometry for selection.
        Assert.True(hardBreak.IsLineBreak);

        ReadOnlySpan<FontRectangle> selection = metrics.GetSelectionBounds(hardBreak).Span;

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
        ReadOnlySpan<LineLayout> lines = block.GetLineLayouts(-1).Span;

        for (int i = 0; i < lines.Length; i++)
        {
            GraphemeMetrics grapheme = lines[i].GraphemeMetrics[0];

            // Reverse line-order modes emit grapheme metrics in visual order, but line metrics
            // retain their source line index. Full-text selection must still find the same
            // owning line slice that the line-local API already has.
            ReadOnlySpan<FontRectangle> expected = lines[i].GetSelectionBounds(grapheme).Span;
            ReadOnlySpan<FontRectangle> actual = metrics.GetSelectionBounds(grapheme).Span;

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

        ReadOnlySpan<LineLayout> lines = block.GetLineLayouts(-1).Span;

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
        ReadOnlySpan<LineLayout> lines = block.GetLineLayouts(-1).Span;
        GraphemeMetrics grapheme = lines[1].GraphemeMetrics[0];
        Vector2 point = FontRectangle.Center(grapheme.Advance);

        TextHit lineHit = lines[1].HitTest(point);
        TextHit metricsHit = metrics.HitTest(point);
        TextHit lineCaretHit = lines[1].HitTest(point);
        TextHit metricsCaretHit = metrics.HitTest(point);
        CaretPosition lineCaret = lines[1].GetCaretPosition(lineCaretHit);
        CaretPosition metricsCaret = metrics.GetCaretPosition(metricsCaretHit);

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
        AssertWordMetrics(metrics.GetWordMetrics(metricsHit), lines[1].GetWordMetrics(lineHit));

        CaretPosition nextMetricsCaret = metrics.MoveCaret(metricsCaret, CaretMovement.Next);
        CaretPosition nextLineCaret = lines[1].MoveCaret(lineCaret, CaretMovement.Next);
        CaretPosition nextWordMetricsCaret = metrics.MoveCaret(metricsCaret, CaretMovement.NextWord);
        CaretPosition nextWordLineCaret = lines[1].MoveCaret(lineCaret, CaretMovement.NextWord);

        Assert.Equal(nextMetricsCaret.GraphemeIndex, nextLineCaret.GraphemeIndex);
        Assert.Equal(nextWordMetricsCaret.GraphemeIndex, nextWordLineCaret.GraphemeIndex);

        Assert.Equal(
            metrics.GetSelectionBounds(grapheme).Span[0],
            lines[1].GetSelectionBounds(grapheme).Span[0],
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
        Assert.True(metrics.WordMetrics.IsEmpty);
        Assert.Equal(FontRectangle.Empty, block.MeasureAdvance(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureBounds(100), Comparer);
        Assert.Equal(FontRectangle.Empty, block.MeasureRenderableBounds(100), Comparer);
        Assert.Equal(0, block.CountLines(100));
        Assert.True(block.GetLineMetrics(100).IsEmpty);
        Assert.True(block.GetWordMetrics(100).IsEmpty);

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

    private static int CountGlyphs(ReadOnlySpan<GlyphBounds> glyphs, CodePoint codePoint)
    {
        int count = 0;
        for (int i = 0; i < glyphs.Length; i++)
        {
            if (glyphs[i].Codepoint == codePoint)
            {
                count++;
            }
        }

        return count;
    }

    private static void AssertSelectionBoundsEqual(ReadOnlySpan<FontRectangle> expected, ReadOnlySpan<FontRectangle> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Comparer);
        }
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
        AssertWordMetricsEqual(expected.WordMetrics, actual.WordMetrics);
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
        }
    }

    private static void AssertWordMetricsEqual(ReadOnlySpan<WordMetrics> expected, ReadOnlySpan<WordMetrics> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertWordMetrics(expected[i], actual[i]);
        }
    }

    private static void AssertWordMetrics(WordMetrics actual, WordMetrics expected)
    {
        Assert.Equal(expected.Advance, actual.Advance, Comparer);
        Assert.Equal(expected.Bounds, actual.Bounds, Comparer);
        Assert.Equal(expected.RenderableBounds, actual.RenderableBounds, Comparer);
        AssertWordMetrics(
            actual,
            expected.GraphemeStart,
            expected.GraphemeEnd,
            expected.StringStart,
            expected.StringEnd);
    }

    private static void AssertWordMetrics(
        WordMetrics actual,
        int graphemeStart,
        int graphemeEnd,
        int stringStart,
        int stringEnd)
    {
        Assert.Equal(graphemeStart, actual.GraphemeStart);
        Assert.Equal(graphemeEnd, actual.GraphemeEnd);
        Assert.Equal(stringStart, actual.StringStart);
        Assert.Equal(stringEnd, actual.StringEnd);
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
