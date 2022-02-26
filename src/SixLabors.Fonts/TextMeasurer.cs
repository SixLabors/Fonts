// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic for laying out and measuring text.
    /// </summary>
    public static class TextMeasurer
    {
        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle Measure(string text, TextOptions options)
            => TextMeasurerInt.Default.Measure(text.AsSpan(), options);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle Measure(ReadOnlySpan<char> text, TextOptions options)
            => TextMeasurerInt.Default.Measure(text, options);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle MeasureBounds(string text, TextOptions options)
            => TextMeasurerInt.Default.MeasureBounds(text.AsSpan(), options);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static FontRectangle MeasureBounds(ReadOnlySpan<char> text, TextOptions options)
            => TextMeasurerInt.Default.MeasureBounds(text, options);

        /// <summary>
        /// Measures the character bounds of the text. For each control character the list contains a <c>null</c> element.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <param name="characterBounds">The list of character bounds of the text if it was to be rendered.</param>
        /// <returns>Whether any of the characters had non-empty bounds.</returns>
        public static bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, TextOptions options, out GlyphBounds[] characterBounds)
            => TextMeasurerInt.Default.TryMeasureCharacterBounds(text, options, out characterBounds);

        /// <summary>
        /// Gets the number of lines contained within the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The line count.</returns>
        public static int CountLines(string text, TextOptions options)
            => TextMeasurerInt.Default.CountLines(text.AsSpan(), options);

        /// <summary>
        /// Gets the number of lines contained within the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The line count.</returns>
        public static int CountLines(ReadOnlySpan<char> text, TextOptions options)
            => TextMeasurerInt.Default.CountLines(text, options);

        internal static FontRectangle GetSize(IReadOnlyList<GlyphLayout> glyphLayouts, float dpi)
        {
            if (glyphLayouts.Count == 0)
            {
                return FontRectangle.Empty;
            }

            float top = glyphLayouts.Min(x => x.Location.Y);
            float left = glyphLayouts.Min(x => x.Location.X);

            // Avoid trimming zero-width marks that extend past the bounds of their base.
            float right = glyphLayouts.Max(x => Math.Max(x.Location.X + x.Width, x.BoundingBox(1F).Right));
            float bottom = glyphLayouts.Max(x => x.Location.Y + x.LineHeight);

            Vector2 topLeft = new Vector2(left, top) * dpi;
            Vector2 bottomRight = new Vector2(right, bottom) * dpi;
            Vector2 size = bottomRight - topLeft;

            return new FontRectangle(0, 0, size.X, size.Y);
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
                hasSize |= !g.IsStartOfLine;
                characterBoundsList[i] = new GlyphBounds(g.Glyph.GlyphMetrics.CodePoint, g.BoundingBox(dpi));
            }

            characterBounds = characterBoundsList;
            return hasSize;
        }

        internal class TextMeasurerInt
        {
            private readonly TextLayout layoutEngine;

            internal TextMeasurerInt(TextLayout layoutEngine)
                => this.layoutEngine = layoutEngine;

            /// <summary>
            /// Initializes a new instance of the <see cref="TextMeasurerInt"/> class.
            /// </summary>
            internal TextMeasurerInt()
            : this(TextLayout.Default)
            {
            }

            internal static TextMeasurerInt Default { get; set; } = new TextMeasurerInt();

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal FontRectangle MeasureBounds(ReadOnlySpan<char> text, TextOptions options)
            {
                IReadOnlyList<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return GetBounds(glyphsToRender, options.Dpi);
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <param name="characterBounds">The character bounds list.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal bool TryMeasureCharacterBounds(ReadOnlySpan<char> text, TextOptions options, out GlyphBounds[] characterBounds)
            {
                IReadOnlyList<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return TryGetCharacterBounds(glyphsToRender, options.Dpi, out characterBounds);
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal FontRectangle Measure(ReadOnlySpan<char> text, TextOptions options)
            {
                IReadOnlyList<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return GetSize(glyphsToRender, options.Dpi);
            }

            /// <summary>
            /// Gets the number of lines contained within the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The line count.</returns>
            internal int CountLines(ReadOnlySpan<char> text, TextOptions options)
            {
                IReadOnlyList<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);
                int usedLines = glyphsToRender.Count(x => x.IsStartOfLine);

                return usedLines;
            }
        }
    }
}
