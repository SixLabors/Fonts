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
    public class TextMeasurer
    {
        internal static TextMeasurer Default { get; set; } = new TextMeasurer();

        private TextLayout layoutEngine;

        internal TextMeasurer(TextLayout layoutEngine)
        {
            this.layoutEngine = layoutEngine;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextMeasurer"/> class.
        /// </summary>
        public TextMeasurer()
            : this(TextLayout.Default)
        {
        }

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="dpi">The dpi.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public Size MeasureText(string text, Font font, float dpi)
        {
            return this.MeasureText(text, new FontSpan(font, dpi));
        }

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="dpi">The dpi.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public Size MeasureText(string text, Font font, Vector2 dpi)
        {
            return this.MeasureText(text, new FontSpan(font, dpi));
        }

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public Size MeasureText(string text, FontSpan style)
        {
            ImmutableArray<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, style);

            return GetSize(glyphsToRender, style.DPI);
        }


        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="dpi">The dpi.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static Size Measure(string text, Font font, float dpi)
            => Default.MeasureText(text, font, dpi);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="dpi">The dpi.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static Size Measure(string text, Font font, Vector2 dpi)
            => Default.MeasureText(text, font, dpi);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static Size Measure(string text, FontSpan style)
            => Default.MeasureText(text, style);

        internal static Size GetSize(ImmutableArray<GlyphLayout> glyphLayouts, Vector2 dpi)
        {
            if (glyphLayouts.IsEmpty)
            {
                return new Size(0, 0);
            }

            return GetBounds(glyphLayouts, dpi).Size();
        }

        internal static Bounds GetBounds(ImmutableArray<GlyphLayout> glyphLayouts, Vector2 dpi)
        {

            float left = glyphLayouts.Min(x => x.Location.X);
            float right = glyphLayouts.Max(x => x.Location.X + x.Width);

            // location is bottom left of the line
            float top = glyphLayouts.Min(x => x.Location.Y);
            float bottom = glyphLayouts.Max(x => x.Location.Y + x.Height);

            Vector2 topLeft = new Vector2(left, top) * dpi;
            Vector2 bottomRight = new Vector2(right, bottom) * dpi;

            return new Bounds(topLeft, bottomRight);
        }
    }
}