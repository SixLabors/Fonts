using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public class FontStyle
    {
        public FontStyle(Font font)
        {
            this.Font = font;
        }

        public float PointSize { get; set; } = 12;

        public Font Font { get; }

        public float TabWidth { get; set; } = 4;

        public bool ApplyKerning { get; set; } = true;

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
                Font = this.Font,
                PointSize = this.PointSize,
                TabWidth = this.TabWidth,
                ApplyKerning = this.ApplyKerning
            };
        }
    }

    internal struct AppliedFontStyle
    {
        public Font Font;
        public float PointSize;
        public float TabWidth;
        public int Start;
        public int End;
        public bool ApplyKerning;
    }
}
