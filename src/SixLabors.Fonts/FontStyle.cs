using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public class FontStyle
    {
        public FontStyle(Font font, float pointSize)
        {
            this.Font = font;
            this.PointSize = pointSize;
        }

        public float PointSize { get; }
        public Font Font { get; }

        /// <summary>
        /// Gets the style. In derived classes this could switchout to different fonts mid stream
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        internal virtual AppliedFontStyle GetStyle(int index, int length)
        {
            return new AppliedFontStyle
            {
                Start = 0,
                End = length - 1,
                Font = Font,
                PointSize = PointSize
            };
        }
    }

    internal struct AppliedFontStyle
    {
        public Font Font;
        public float PointSize;
        public int Start;
        public int End;
    }
}
