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
        private TextLayout layoutEngine;

        internal TextMeasurer(TextLayout layoutEngine)
        {
            this.layoutEngine = layoutEngine;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextMeasurer"/> class.
        /// </summary>
        public TextMeasurer()
            : this(new TextLayout())
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
        /// <param name="dpi">The dpi.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public Size MeasureText(string text, FontSpan style)
        {
            var glyphsToRender = this.layoutEngine.GenerateLayout(text, style);
            
            var left = glyphsToRender.Min(x => x.Location.X);
            var right = glyphsToRender.Max(x => x.Location.X + x.Width);

            var top = glyphsToRender.Min(x => x.Location.Y);
            var bottom = glyphsToRender.Max(x => x.Location.Y + x.Height);

            var topLeft = new Vector2(left, top) * style.DPI;
            var bottomRight = new Vector2(right, bottom) * style.DPI;

            return new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }
    }
}
