// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic for laying out and then measuring text properties.
    /// </summary>
    public static class TextMeasurer
    {
        /// <summary>
        /// Measures the size of the text in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle Measure(string text, TextOptions options)
            => Measure(text.AsSpan(), options);

        /// <summary>
        /// Measures the size of the text in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle Measure(ReadOnlySpan<char> text, TextOptions options)
            => GetSize(TextLayout.GenerateLayout(text, options), options.Dpi);

        /// <summary>
        /// Measures the text bounds in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle MeasureBounds(string text, TextOptions options)
            => MeasureBounds(text.AsSpan(), options);

        /// <summary>
        /// Measures the text bounds in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle MeasureBounds(ReadOnlySpan<char> text, TextOptions options)
            => GetBounds(TextLayout.GenerateLayout(text, options), options.Dpi);

        /// <summary>
        /// Measures the size of each character of the text in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <param name="characterBounds">The list of character dimensions of the text if it was to be rendered.</param>
        /// <returns>Whether any of the characters had non-empty dimensions.</returns>
        public static bool TryMeasureCharacterDimensions(string text, TextOptions options, out GlyphBounds[] characterBounds)
            => TryMeasureCharacterDimensions(text.AsSpan(), options, out characterBounds);

        /// <summary>
        /// Measures the size of each character of the text in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <param name="characterBounds">The list of character dimensions of the text if it was to be rendered.</param>
        /// <returns>Whether any of the characters had non-empty dimensions.</returns>
        public static bool TryMeasureCharacterDimensions(ReadOnlySpan<char> text, TextOptions options, out GlyphBounds[] characterBounds)
            => TryGetCharacterDimensions(TextLayout.GenerateLayout(text, options), options.Dpi, out characterBounds);

        /// <summary>
        /// Measures the character bounds of the text in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <param name="characterBounds">The list of character bounds of the text if it was to be rendered.</param>
        /// <returns>Whether any of the characters had non-empty bounds.</returns>
        public static bool TryMeasureCharacterBounds(string text, TextOptions options, out GlyphBounds[] characterBounds)
            => TryMeasureCharacterBounds(text.AsSpan(), options, out characterBounds);

        /// <summary>
        /// Measures the character bounds of the text in pixel units.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The text shaping options.</param>
        /// <param name="characterBounds">The list of character bounds of the text if it was to be rendered.</param>
        /// <returns>Whether any of the characters had non-empty bounds.</returns>
        public static bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, TextOptions options, out GlyphBounds[] characterBounds)
            => TryGetCharacterBounds(TextLayout.GenerateLayout(text, options), options.Dpi, out characterBounds);

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

        internal static FontRectangle GetSize(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi)
        {
            if (glyphLayouts.Count == 0)
            {
                return FontRectangle.Empty;
            }

            float left = int.MaxValue;
            float top = int.MaxValue;
            float bottom = int.MinValue;
            float right = int.MinValue;

            for (int i = 0; i < glyphLayouts.Count; i++)
            {
                GlyphLayout glyph = glyphLayouts[i];
                Vector2 location = glyph.Location;
                float x = location.X;
                float y = location.Y - glyph.Ascender;
                float lineHeight = glyph.LineHeight;

                // Avoid trimming zero-width/height marks that extend past the bounds of their base.
                FontRectangle box = glyph.BoundingBox(1F);
                float advanceX = Math.Max(x + glyph.Width, box.Right);
                float advanceY = Math.Max(y + lineHeight, box.Top + box.Height);

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

            Vector2 topLeft = new Vector2(left, top) * dpi;
            Vector2 bottomRight = new Vector2(right, bottom) * dpi;
            Vector2 size = bottomRight - topLeft;

            return new FontRectangle(0, 0, MathF.Ceiling(size.X), MathF.Ceiling(size.Y));
        }

        internal static FontRectangle GetBounds(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi)
        {
            if (glyphLayouts.Count == 0)
            {
                return FontRectangle.Empty;
            }

            float left = int.MaxValue;
            float top = int.MaxValue;
            float bottom = int.MinValue;
            float right = int.MinValue;
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

        internal static bool TryGetCharacterDimensions(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi, out GlyphBounds[] characterBounds)
        {
            bool hasSize = false;
            if (glyphLayouts.Count == 0)
            {
                characterBounds = Array.Empty<GlyphBounds>();
                return hasSize;
            }

            var characterBoundsList = new GlyphBounds[glyphLayouts.Count];
            for (int i = 0; i < glyphLayouts.Count; i++)
            {
                GlyphLayout glyph = glyphLayouts[i];
                FontRectangle bounds = new(0, 0, glyph.Width * dpi, glyph.Height * dpi);
                hasSize |= bounds.Width > 0 || bounds.Height > 0;
                characterBoundsList[i] = new GlyphBounds(glyph.Glyph.GlyphMetrics.CodePoint, in bounds);
            }

            characterBounds = characterBoundsList;
            return hasSize;
        }

        internal static bool TryGetCharacterBounds(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi, out GlyphBounds[] characterBounds)
        {
            bool hasSize = false;
            if (glyphLayouts.Count == 0)
            {
                characterBounds = Array.Empty<GlyphBounds>();
                return hasSize;
            }

            var characterBoundsList = new GlyphBounds[glyphLayouts.Count];
            for (int i = 0; i < glyphLayouts.Count; i++)
            {
                GlyphLayout g = glyphLayouts[i];
                FontRectangle bounds = g.BoundingBox(dpi);
                hasSize |= bounds.Width > 0 || bounds.Height > 0;
                characterBoundsList[i] = new GlyphBounds(g.Glyph.GlyphMetrics.CodePoint, in bounds);
            }

            characterBounds = characterBoundsList;
            return hasSize;
        }
    }
}
