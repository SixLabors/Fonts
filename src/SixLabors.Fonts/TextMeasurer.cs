using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public class TextMeasurer
    {
        private TextLayout layoutEngine;

        internal TextMeasurer(TextLayout layoutEngine)
        {
            this.layoutEngine = layoutEngine;
        }

        public TextMeasurer()
            : this(new TextLayout())
        {
        }

        public Size MeasureText(string text, Font font, float pointSize, float dpi)
        {
            return MeasureText(text, new FontStyle(font) { PointSize = pointSize }, new Vector2(dpi));
        }
        public Size MeasureText(string text, Font font, float pointSize, Vector2 dpi)
        {
            return MeasureText(text, new FontStyle(font) { PointSize = pointSize }, dpi);
        }
        public Size MeasureText(string text, FontStyle style, float dpi)
        {
            return MeasureText(text, style, new Vector2(dpi));
        }

        public Size MeasureText(string text, FontStyle style, Vector2 dpi)
        {
            var glyphsToRender = layoutEngine.GenerateLayout(text, style);

            var left = glyphsToRender.Min(x => x.Location.X);
            var right = glyphsToRender.Max(x => x.Location.X + x.Width);

            var top = glyphsToRender.Min(x => x.Location.Y);
            var bottom = glyphsToRender.Max(x => x.Location.Y + x.Height);

            var topLeft = new Vector2(left, top) * dpi;
            var bottomRight = new Vector2(right, bottom) * dpi;

            return new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }
    }
}
