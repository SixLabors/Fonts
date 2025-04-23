// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Encapsulated logic for laying out and then measuring text properties.
/// </summary>
public static class TextMeasurer
{
    /// <summary>
    /// Measures the advance (line-height and horizontal/vertical advance) of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The advance of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureAdvance(string text, TextOptions options)
        => MeasureAdvance(text.AsSpan(), options);

    /// <summary>
    /// Measures the advance (line-height and horizontal/vertical advance) of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The advance of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureAdvance(ReadOnlySpan<char> text, TextOptions options)
        => GetAdvance(TextLayout.GenerateLayout(text, options), options.Dpi);

    /// <summary>
    /// Measures the minimum size required, in pixel units, to render the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The size of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureSize(string text, TextOptions options)
        => MeasureSize(text.AsSpan(), options);

    /// <summary>
    /// Measures the minimum size required, in pixel units, to render the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The size of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureSize(ReadOnlySpan<char> text, TextOptions options)
        => GetSize(TextLayout.GenerateLayout(text, options), options.Dpi);

    /// <summary>
    /// Measures the text bounds in sub-pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The bounds of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureBounds(string text, TextOptions options)
        => MeasureBounds(text.AsSpan(), options);

    /// <summary>
    /// Measures the text bounds in sub-pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The bounds of the text if it was to be rendered.</returns>
    public static FontRectangle MeasureBounds(ReadOnlySpan<char> text, TextOptions options)
        => GetBounds(TextLayout.GenerateLayout(text, options), options.Dpi);

    /// <summary>
    /// Measures the advance (line-height and horizontal/vertical advance) of each character of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="advances">The list of character advances of the text if it was to be rendered.</param>
    /// <returns>Whether any of the characters had non-empty advances.</returns>
    public static bool TryMeasureCharacterAdvances(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> advances)
        => TryMeasureCharacterAdvances(text.AsSpan(), options, out advances);

    /// <summary>
    /// Measures the advance (line-height and horizontal/vertical advance) of each character of the text in pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="advances">The list of character advances of the text if it was to be rendered.</param>
    /// <returns>Whether any of the characters had non-empty advances.</returns>
    public static bool TryMeasureCharacterAdvances(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> advances)
        => TryGetCharacterAdvances(TextLayout.GenerateLayout(text, options), options.Dpi, out advances);

    /// <summary>
    /// Measures the minimum size required, in pixel units, to render each character in the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="sizes">The list of character dimensions of the text if it was to be rendered.</param>
    /// <returns>Whether any of the characters had non-empty dimensions.</returns>
    public static bool TryMeasureCharacterSizes(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> sizes)
        => TryMeasureCharacterSizes(text.AsSpan(), options, out sizes);

    /// <summary>
    /// Measures the minimum size required, in pixel units, to render each character in the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="sizes">The list of character dimensions of the text if it was to be rendered.</param>
    /// <returns>Whether any of the characters had non-empty dimensions.</returns>
    public static bool TryMeasureCharacterSizes(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> sizes)
        => TryGetCharacterSizes(TextLayout.GenerateLayout(text, options), options.Dpi, out sizes);

    /// <summary>
    /// Measures the character bounds of the text in sub-pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="bounds">The list of character bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the characters had non-empty bounds.</returns>
    public static bool TryMeasureCharacterBounds(string text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryMeasureCharacterBounds(text.AsSpan(), options, out bounds);

    /// <summary>
    /// Measures the character bounds of the text in sub-pixel units.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <param name="bounds">The list of character bounds of the text if it was to be rendered.</param>
    /// <returns>Whether any of the characters had non-empty bounds.</returns>
    public static bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, TextOptions options, out ReadOnlySpan<GlyphBounds> bounds)
        => TryGetCharacterBounds(TextLayout.GenerateLayout(text, options), options.Dpi, out bounds);

    /// <summary>
    /// Gets the number of lines contained within the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The line count.</returns>
    public static int CountLines(string text, TextOptions options)
        => CountLines(text.AsSpan(), options);

    /// <summary>
    /// Gets the number of lines contained within the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="options">The text shaping options.</param>
    /// <returns>The line count.</returns>
    public static int CountLines(ReadOnlySpan<char> text, TextOptions options)
        => TextLayout.GenerateLayout(text, options).Count(x => x.IsStartOfLine);

    internal static FontRectangle GetAdvance(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi)
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
            GlyphLayout glyph = glyphLayouts[i];
            Vector2 location = glyph.PenLocation;
            float x = location.X;
            float y = location.Y;

            if (glyph.IsStartOfLine)
            {
                // When the text contains a mix of glyphs of different sizes there can be line position overlap.
                // To accurately measure we offset to ensure that we always start where the last line ended.
                // Glyphs are always laid out from top-left to bottom-right so we can simply use the max.
                if (glyph.LayoutMode == GlyphLayoutMode.Horizontal)
                {
                    y = MathF.Max(y, bottom);
                }
                else
                {
                    x = MathF.Max(x, right);
                }
            }

            float advanceX = x + glyph.AdvanceX;
            float advanceY = y + glyph.AdvanceY;

            if (left > x)
            {
                left = x;
            }

            if (top > y)
            {
                top = y;
            }

            if (right < advanceX)
            {
                right = advanceX;
            }

            if (bottom < advanceY)
            {
                bottom = advanceY;
            }
        }

        Vector2 topLeft = new(left, top);
        Vector2 bottomRight = new(right, bottom);
        Vector2 size = (bottomRight - topLeft) * dpi;
        return new FontRectangle(0, 0, size.X, size.Y);
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
            characterBounds = Array.Empty<GlyphBounds>();
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
            characterBounds = Array.Empty<GlyphBounds>();
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
            characterBounds = Array.Empty<GlyphBounds>();
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
}
