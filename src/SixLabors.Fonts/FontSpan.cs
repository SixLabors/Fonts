using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// The font style to render onto a peice of text.
    /// </summary>
    public class FontSpan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontSpan"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        public FontSpan(Font font)
        {
            this.Font = font;
        }

        /// <summary>
        /// Gets the font.
        /// </summary>
        /// <value>
        /// The font.
        /// </value>
        public Font Font { get; }

        /// <summary>
        /// Gets or sets the width of the tab.
        /// </summary>
        /// <value>
        /// The width of the tab.
        /// </value>
        public float TabWidth { get; set; } = 4;

        /// <summary>
        /// Gets or sets a value indicating whether [apply kerning].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [apply kerning]; otherwise, <c>false</c>.
        /// </value>
        public bool ApplyKerning { get; set; } = true;

        /// <summary>
        /// Gets the style. In derived classes this could switchout to different fonts mid stream
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The Font style that applies to a region of text.
        /// </returns>
        internal virtual AppliedFontStyle GetStyle(int index, int length)
        {
            return new AppliedFontStyle
            {
                Start = 0,
                End = length - 1,
                PointSize = this.Font.Size,
                Font = this.Font.FontInstance,
                TabWidth = this.TabWidth,
                ApplyKerning = this.ApplyKerning
            };
        }
    }

    internal struct AppliedFontStyle
    {
        public IFontInstance Font;
        public float PointSize;
        public float TabWidth;
        public int Start;
        public int End;
        public bool ApplyKerning;
    }
}
