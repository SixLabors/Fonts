// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulated logic for laying out and then measuring text properties.
/// </summary>
public static class TextMeasurer
{
    /// <summary>
    /// Measures the logical advance of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The logical advance rectangle of the text if it was to be rendered.</returns>
    /// <remarks>
    /// This measurement reflects line-box height and horizontal or vertical text advance from the layout model.
    /// It does not guarantee that all rendered glyph pixels fit within the returned rectangle.
    /// Use <see cref="MeasureBounds(string, TextOptions)"/> for glyph ink bounds or
    /// <see cref="MeasureRenderableBounds(string, TextOptions)"/> for the union of logical advance and rendered bounds.
    /// </remarks>
    public static FontRectangle MeasureAdvance(string text, TextOptions options)
        => MeasureAdvance(text.AsSpan(), options);

    /// <summary>
    /// Measures the logical advance of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The logical advance rectangle of the text if it was to be rendered.</returns>
    /// <remarks>
    /// This measurement reflects line-box height and horizontal or vertical text advance from the layout model.
    /// It does not guarantee that all rendered glyph pixels fit within the returned rectangle.
    /// Use <see cref="MeasureBounds(ReadOnlySpan{char}, TextOptions)"/> for glyph ink bounds or
    /// <see cref="MeasureRenderableBounds(ReadOnlySpan{char}, TextOptions)"/> for the union of logical advance and rendered bounds.
    /// </remarks>
    public static FontRectangle MeasureAdvance(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return FontRectangle.Empty;
        }

        return GetAdvance(TextLayout.ProcessText(text, options), options.Dpi, options.LayoutMode.IsHorizontal());
    }

    /// <summary>
    /// Measures the normalized rendered size of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The rendered size of the text with the origin normalized to <c>(0, 0)</c>.</returns>
    /// <remarks>
    /// This is equivalent to measuring the rendered bounds and returning only the width and height.
    /// Use <see cref="MeasureBounds(string, TextOptions)"/> when the returned X and Y offset are also required.
    /// </remarks>
    public static FontRectangle MeasureSize(string text, TextOptions options)
        => MeasureSize(text.AsSpan(), options);

    /// <summary>
    /// Measures the normalized rendered size of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The rendered size of the text with the origin normalized to <c>(0, 0)</c>.</returns>
    /// <remarks>
    /// This is equivalent to measuring the rendered bounds and returning only the width and height.
    /// Use <see cref="MeasureBounds(ReadOnlySpan{char}, TextOptions)"/> when the returned X and Y offset are also required.
    /// </remarks>
    public static FontRectangle MeasureSize(ReadOnlySpan<char> text, TextOptions options)
        => GetSize(TextLayout.GenerateLayout(text, options), options.Dpi);

    /// <summary>
    /// Measures the rendered glyph bounds of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The rendered glyph bounds of the text if it was to be rendered.</returns>
    /// <remarks>
    /// This measures the tight ink bounds enclosing all rendered glyphs. The returned rectangle
    /// may be smaller or larger than the logical advance and may have a non-zero origin.
    /// Use <see cref="MeasureAdvance(string, TextOptions)"/> for the logical layout box or
    /// <see cref="MeasureRenderableBounds(string, TextOptions)"/> for the union of both.
    /// </remarks>
    public static FontRectangle MeasureBounds(string text, TextOptions options)
        => MeasureBounds(text.AsSpan(), options);

    /// <summary>
    /// Measures the full renderable bounds of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>
    /// The union of the logical advance rectangle and the rendered glyph bounds if the text was to be rendered.
    /// </returns>
    /// <remarks>
    /// The returned rectangle is in absolute coordinates and is large enough to contain both the logical advance
    /// rectangle and the rendered glyph bounds.
    /// Use this method when both typographic advance and rendered glyph overshoot must fit within the same rectangle.
    /// </remarks>
    public static FontRectangle MeasureRenderableBounds(string text, TextOptions options)
        => MeasureRenderableBounds(text.AsSpan(), options);

    /// <summary>
    /// Measures the rendered glyph bounds of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The rendered glyph bounds of the text if it was to be rendered.</returns>
    /// <remarks>
    /// This measures the tight ink bounds enclosing all rendered glyphs. The returned rectangle
    /// may be smaller or larger than the logical advance and may have a non-zero origin.
    /// Use <see cref="MeasureAdvance(ReadOnlySpan{char}, TextOptions)"/> for the logical layout box or
    /// <see cref="MeasureRenderableBounds(ReadOnlySpan{char}, TextOptions)"/> for the union of both.
    /// </remarks>
    public static FontRectangle MeasureBounds(ReadOnlySpan<char> text, TextOptions options)
        => GetBounds(TextLayout.GenerateLayout(text, options), options.Dpi);

    /// <summary>
    /// Measures the full renderable bounds of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>
    /// The union of the logical advance rectangle and the rendered glyph bounds if the text was to be rendered.
    /// </returns>
    /// <remarks>
    /// The returned rectangle is in absolute coordinates and is large enough to contain both the logical advance
    /// rectangle and the rendered glyph bounds.
    /// Use this method when both typographic advance and rendered glyph overshoot must fit within the same rectangle.
    /// </remarks>
    public static FontRectangle MeasureRenderableBounds(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return FontRectangle.Empty;
        }

        FontRectangle advance = MeasureAdvance(text, options);
        FontRectangle absoluteAdvance = new(options.Origin.X, options.Origin.Y, advance.Width, advance.Height);
        FontRectangle bounds = MeasureBounds(text, options);
        return FontRectangle.Union(absoluteAdvance, bounds);
    }

    /// <summary>
    /// Measures the logical advance of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="advances">The list of per-entry logical advances of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty advances.</returns>
    /// <remarks>
    /// Each entry reflects the typographic advance width and height for one character.
    /// Use <see cref="TryMeasureCharacterBounds(string, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for per-character ink bounds or
    /// <see cref="TryMeasureCharacterRenderableBounds(string, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for the union of both.
    /// </remarks>
    public static bool TryMeasureCharacterAdvances(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> advances)
        => TryMeasureCharacterAdvances(text.AsSpan(), options, out advances);

    /// <summary>
    /// Measures the logical advance of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="advances">The list of per-entry logical advances of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty advances.</returns>
    /// <remarks>
    /// Each entry reflects the typographic advance width and height for one character.
    /// Use <see cref="TryMeasureCharacterBounds(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for per-character ink bounds or
    /// <see cref="TryMeasureCharacterRenderableBounds(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for the union of both.
    /// </remarks>
    public static bool TryMeasureCharacterAdvances(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> advances)
        => TryGetCharacterAdvances(TextLayout.GenerateLayout(text, options), options.Dpi, out advances);

    /// <summary>
    /// Measures the normalized rendered size of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="sizes">The list of per-entry rendered sizes with the origin normalized to <c>(0, 0)</c>.</param>
    /// <returns>Whether any of the entries had non-empty dimensions.</returns>
    public static bool TryMeasureCharacterSizes(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> sizes)
        => TryMeasureCharacterSizes(text.AsSpan(), options, out sizes);

    /// <summary>
    /// Measures the normalized rendered size of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="sizes">The list of per-entry rendered sizes with the origin normalized to <c>(0, 0)</c>.</param>
    /// <returns>Whether any of the entries had non-empty dimensions.</returns>
    public static bool TryMeasureCharacterSizes(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> sizes)
        => TryGetCharacterSizes(TextLayout.GenerateLayout(text, options), options.Dpi, out sizes);

    /// <summary>
    /// Measures the rendered glyph bounds of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="bounds">The list of per-entry rendered glyph bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    /// <remarks>
    /// Each entry reflects the tight ink bounds of one rendered glyph.
    /// Use <see cref="TryMeasureCharacterAdvances(string, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for per-character logical advances or
    /// <see cref="TryMeasureCharacterRenderableBounds(string, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for the union of both.
    /// </remarks>
    public static bool TryMeasureCharacterBounds(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryMeasureCharacterBounds(text.AsSpan(), options, out bounds);

    /// <summary>
    /// Measures the full renderable bounds of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="bounds">The list of per-entry renderable bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    /// <remarks>
    /// Each returned rectangle is in absolute coordinates and is large enough to contain both the logical advance
    /// rectangle and the rendered glyph bounds for the corresponding laid-out entry.
    /// </remarks>
    public static bool TryMeasureCharacterRenderableBounds(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryMeasureCharacterRenderableBounds(text.AsSpan(), options, out bounds);

    /// <summary>
    /// Measures the rendered glyph bounds of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="bounds">The list of per-entry rendered glyph bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    /// <remarks>
    /// Each entry reflects the tight ink bounds of one rendered glyph.
    /// Use <see cref="TryMeasureCharacterAdvances(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for per-character logical advances or
    /// <see cref="TryMeasureCharacterRenderableBounds(ReadOnlySpan{char}, TextOptions, out ReadOnlySpan{GlyphBounds})"/> for the union of both.
    /// </remarks>
    public static bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryGetCharacterBounds(TextLayout.GenerateLayout(text, options), options.Dpi, out bounds);

    /// <summary>
    /// Measures the full renderable bounds of each laid-out character entry in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="bounds">The list of per-entry renderable bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the entries had non-empty bounds.</returns>
    /// <remarks>
    /// Each returned rectangle is in absolute coordinates and is large enough to contain both the logical advance
    /// rectangle and the rendered glyph bounds for the corresponding laid-out entry.
    /// </remarks>
    public static bool TryMeasureCharacterRenderableBounds(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryGetCharacterRenderableBounds(TextLayout.GenerateLayout(text, options), options.Dpi, out bounds);

    /// <summary>
    /// Gets the number of laid-out lines contained within the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The laid-out line count.</returns>
    public static int CountLines(string text, TextOptions options)
        => CountLines(text.AsSpan(), options);

    /// <summary>
    /// Gets the number of laid-out lines contained within the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The laid-out line count.</returns>
    public static int CountLines(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        return TextLayout.ProcessText(text, options).TextLines.Count;
    }

    /// <summary>
    /// Gets per-line layout metrics for the supplied text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="options">The text shaping and layout options.</param>
    /// <returns>
    /// An array of <see cref="LineMetrics"/> in pixel units, one entry per laid-out line.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned <see cref="LineMetrics.Start"/> and <see cref="LineMetrics.Extent"/> are expressed
    /// in the primary flow direction for the active layout mode.
    /// </para>
    /// <para>
    /// <see cref="LineMetrics.Ascender"/>, <see cref="LineMetrics.Baseline"/>, and <see cref="LineMetrics.Descender"/>
    /// are line-box positions relative to the current line origin and are suitable for drawing guide lines.
    /// </para>
    /// <list type="bullet">
    /// <item><description>Horizontal layouts: Start = X position, Extent = width.</description></item>
    /// <item><description>Vertical layouts: Start = Y position, Extent = height.</description></item>
    /// </list>
    /// </remarks>
    public static LineMetrics[] GetLineMetrics(string text, TextOptions options)
        => GetLineMetrics(text.AsSpan(), options);

    /// <summary>
    /// Gets per-line layout metrics for the supplied text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="options">The text shaping and layout options.</param>
    /// <returns>
    /// An array of <see cref="LineMetrics"/> in pixel units, one entry per laid-out line.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned <see cref="LineMetrics.Start"/> and <see cref="LineMetrics.Extent"/> are expressed
    /// in the primary flow direction for the active layout mode.
    /// </para>
    /// <para>
    /// <see cref="LineMetrics.Ascender"/>, <see cref="LineMetrics.Baseline"/>, and <see cref="LineMetrics.Descender"/>
    /// are line-box positions relative to the current line origin and are suitable for drawing guide lines.
    /// </para>
    /// <list type="bullet">
    /// <item><description>Horizontal layouts: Start = X position, Extent = width.</description></item>
    /// <item><description>Vertical layouts: Start = Y position, Extent = height.</description></item>
    /// </list>
    /// </remarks>
    public static LineMetrics[] GetLineMetrics(ReadOnlySpan<char> text, TextOptions options)
    {
        if (text.IsEmpty)
        {
            return [];
        }

        TextLayout.TextBox textBox = TextLayout.ProcessText(text, options);
        LineMetrics[] metrics = new LineMetrics[textBox.TextLines.Count];

        // Determine the line-box extent used for alignment within the flow direction.
        float maxScaledAdvance = textBox.ScaledMaxAdvance();
        if (options.TextAlignment != TextAlignment.Start && options.WrappingLength > 0)
        {
            maxScaledAdvance = MathF.Max(options.WrappingLength / options.Dpi, maxScaledAdvance);
        }

        TextDirection direction = textBox.TextDirection();
        LayoutMode layoutMode = options.LayoutMode;
        bool isHorizontalLayout = layoutMode.IsHorizontal();

        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            TextLayout.TextLine line = textBox.TextLines[i];

            // Calculate the line start position in the current flow direction.
            float offset = isHorizontalLayout
                ? TextLayout.CalculateLineOffsetX(
                    line.ScaledLineAdvance,
                    maxScaledAdvance,
                    options.HorizontalAlignment,
                    options.TextAlignment,
                    direction)
                : TextLayout.CalculateLineOffsetY(
                    line.ScaledLineAdvance,
                    maxScaledAdvance,
                    options.VerticalAlignment,
                    options.TextAlignment,
                    direction);

            // Delta captured during layout when ascender/descender were symmetrically
            // adjusted to match browser-like line-box behavior.
            float delta = line.ScaledMaxDelta;

            // Core typographic region within the line box.
            // We add back 2*delta to recover the pre-adjustment ascender+descender span
            // used for deriving guide positions.
            float coreHeight = line.ScaledMaxAscender + line.ScaledMaxDescender + (2 * delta);

            // Additional leading in the line box (for example from line spacing).
            float extra = line.ScaledMaxLineHeight - coreHeight;

            // Baseline position within the line box.
            float baseline = (extra * 0.5f) + line.ScaledMaxAscender + delta;

            // Ascender line position relative to the same origin.
            float ascender = baseline - line.ScaledMaxAscender + delta;

            // Descender line position relative to the same origin.
            float descender = baseline + line.ScaledMaxDescender + delta;

            metrics[i] = new LineMetrics(
                ascender * options.Dpi,
                baseline * options.Dpi,
                descender * options.Dpi,
                line.ScaledMaxLineHeight * options.Dpi,
                offset * options.Dpi,
                line.ScaledLineAdvance * options.Dpi);
        }

        return metrics;
    }

    internal static FontRectangle GetAdvance(TextLayout.TextBox textBox, float dpi, bool isHorizontalLayout)
    {
        if (textBox.TextLines.Count == 0)
        {
            return FontRectangle.Empty;
        }

        if (isHorizontalLayout)
        {
            float width = 0;
            float height = 0;
            for (int i = 0; i < textBox.TextLines.Count; i++)
            {
                TextLayout.TextLine line = textBox.TextLines[i];
                width = MathF.Max(width, line.ScaledLineAdvance);
                height += line.ScaledMaxLineHeight;
            }

            return new FontRectangle(0, 0, width * dpi, height * dpi);
        }

        float verticalWidth = 0;
        float verticalHeight = 0;
        for (int i = 0; i < textBox.TextLines.Count; i++)
        {
            TextLayout.TextLine line = textBox.TextLines[i];
            verticalWidth += line.ScaledMaxLineHeight;
            verticalHeight = MathF.Max(verticalHeight, line.ScaledLineAdvance);
        }

        return new FontRectangle(0, 0, verticalWidth * dpi, verticalHeight * dpi);
    }

    internal static FontRectangle GetSize(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi)
    {
        FontRectangle bounds = GetBounds(glyphLayouts, dpi);
        return new FontRectangle(0, 0, bounds.Width, bounds.Height);
    }

    internal static FontRectangle GetBounds(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi)
    {
        if (glyphLayouts.Count == 0)
        {
            return FontRectangle.Empty;
        }

        float left = float.MaxValue;
        float top = float.MaxValue;
        float bottom = float.MinValue;
        float right = float.MinValue;
        for (int i = 0; i < glyphLayouts.Count; i++)
        {
            FontRectangle box = glyphLayouts[i].BoundingBox(dpi);
            if (left > box.Left)
            {
                left = box.Left;
            }

            if (top > box.Top)
            {
                top = box.Top;
            }

            if (bottom < box.Bottom)
            {
                bottom = box.Bottom;
            }

            if (right < box.Right)
            {
                right = box.Right;
            }
        }

        return FontRectangle.FromLTRB(left, top, right, bottom);
    }

    internal static bool TryGetCharacterAdvances(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi, out ReadOnlySpan<GlyphBounds> characterBounds)
    {
        bool hasSize = false;
        if (glyphLayouts.Count == 0)
        {
            characterBounds = [];
            return hasSize;
        }

        GlyphBounds[] characterBoundsList = new GlyphBounds[glyphLayouts.Count];
        for (int i = 0; i < glyphLayouts.Count; i++)
        {
            GlyphLayout glyph = glyphLayouts[i];
            FontRectangle bounds = new(0, 0, glyph.AdvanceX * dpi, glyph.AdvanceY * dpi);
            hasSize |= bounds.Width > 0 || bounds.Height > 0;
            characterBoundsList[i] = new GlyphBounds(glyph.Glyph.GlyphMetrics.CodePoint, in bounds, glyph.GraphemeIndex, glyph.StringIndex);
        }

        characterBounds = characterBoundsList;
        return hasSize;
    }

    internal static bool TryGetCharacterSizes(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi, out ReadOnlySpan<GlyphBounds> characterBounds)
    {
        bool hasSize = false;
        if (glyphLayouts.Count == 0)
        {
            characterBounds = [];
            return hasSize;
        }

        GlyphBounds[] characterBoundsList = new GlyphBounds[glyphLayouts.Count];

        for (int i = 0; i < glyphLayouts.Count; i++)
        {
            GlyphLayout g = glyphLayouts[i];
            FontRectangle bounds = g.BoundingBox(dpi);
            bounds = new(0, 0, bounds.Width, bounds.Height);

            hasSize |= bounds.Width > 0 || bounds.Height > 0;
            characterBoundsList[i] = new GlyphBounds(g.Glyph.GlyphMetrics.CodePoint, in bounds, g.GraphemeIndex, g.StringIndex);
        }

        characterBounds = characterBoundsList;
        return hasSize;
    }

    internal static bool TryGetCharacterBounds(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi, out ReadOnlySpan<GlyphBounds> characterBounds)
    {
        bool hasSize = false;
        if (glyphLayouts.Count == 0)
        {
            characterBounds = [];
            return hasSize;
        }

        GlyphBounds[] characterBoundsList = new GlyphBounds[glyphLayouts.Count];
        for (int i = 0; i < glyphLayouts.Count; i++)
        {
            GlyphLayout g = glyphLayouts[i];
            FontRectangle bounds = g.BoundingBox(dpi);
            hasSize |= bounds.Width > 0 || bounds.Height > 0;
            characterBoundsList[i] = new GlyphBounds(g.Glyph.GlyphMetrics.CodePoint, in bounds, g.GraphemeIndex, g.StringIndex);
        }

        characterBounds = characterBoundsList;
        return hasSize;
    }

    internal static bool TryGetCharacterRenderableBounds(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi, out ReadOnlySpan<GlyphBounds> characterBounds)
    {
        bool hasSize = false;
        if (glyphLayouts.Count == 0)
        {
            characterBounds = [];
            return hasSize;
        }

        GlyphBounds[] characterBoundsList = new GlyphBounds[glyphLayouts.Count];
        for (int i = 0; i < glyphLayouts.Count; i++)
        {
            GlyphLayout g = glyphLayouts[i];
            FontRectangle glyphBounds = g.BoundingBox(dpi);
            FontRectangle advance = new(g.BoxLocation.X * dpi, g.BoxLocation.Y * dpi, g.AdvanceX * dpi, g.AdvanceY * dpi);
            FontRectangle bounds = FontRectangle.Union(advance, glyphBounds);
            hasSize |= bounds.Width > 0 || bounds.Height > 0;
            characterBoundsList[i] = new GlyphBounds(g.Glyph.GlyphMetrics.CodePoint, in bounds, g.GraphemeIndex, g.StringIndex);
        }

        characterBounds = characterBoundsList;
        return hasSize;
    }
}
