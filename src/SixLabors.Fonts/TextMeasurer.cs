using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapulated logic for laying out and measuring text.
    /// </summary>
    public static class TextMeasurer
    {
        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static RectangleF Measure(string text, RendererOptions options)
            => TextMeasurerInt.Default.Measure(text, options);

        internal static RectangleF GetBounds(ImmutableArray<GlyphLayout> glyphLayouts, Vector2 dpi)
        {
            if (glyphLayouts.IsEmpty)
            {
                return RectangleF.Empty;
            }

            bool hasSize = false;

            float left = int.MaxValue;
            float top = int.MaxValue;
            float bottom = int.MinValue;
            float right = int.MinValue;

            for (var i = 0; i < glyphLayouts.Length; i++)
            {
                var c = glyphLayouts[i];
                if (!c.IsControlCharacter)
                {
                    hasSize = true;
                    var box = c.BoundingBox(dpi);
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
            }

            if (!hasSize)
            {
                return RectangleF.Empty;
            }

            var width = right - left;
            var height = bottom - top;

            return new RectangleF(left, top, width, height);
        }

        internal class TextMeasurerInt
        {
            internal static TextMeasurerInt Default { get; set; } = new TextMeasurerInt();

            private TextLayout layoutEngine;

            internal TextMeasurerInt(TextLayout layoutEngine)
            {
                this.layoutEngine = layoutEngine;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TextMeasurer"/> class.
            /// </summary>
            internal TextMeasurerInt()
            : this(TextLayout.Default)
            {
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal RectangleF Measure(string text, RendererOptions options)
            {
                ImmutableArray<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return GetBounds(glyphsToRender, new Vector2(options.DpiX, options.DpiY));
            }
        }
    }
}