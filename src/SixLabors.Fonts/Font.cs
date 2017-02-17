using System;
using System.IO;
using System.Numerics;

using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a particular format for text, including font face, size, and style attributes. This class cannot be inherited.
    /// </summary>
    public sealed class Font
    {
        private readonly FontStyle requestedStyle;
        private readonly Lazy<IFontInstance> instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        public Font(FontFamily family, float size, FontStyle style)
        {
            this.Family = family;
            this.requestedStyle = style;
            this.Size = size;
            this.instance = new Lazy<IFontInstance>(this.LoadInstanceInternal);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="size">The size.</param>
        public Font(FontFamily family, float size)
            : this(family, size, FontStyle.Regular)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="style">The style.</param>
        public Font(Font prototype, FontStyle style)
            : this(prototype.Family, prototype.Size, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        public Font(Font prototype, float size, FontStyle style)
            : this(prototype.Family, size, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="size">The size.</param>
        public Font(Font prototype, float size)
            : this(prototype.Family, size, prototype.requestedStyle)
        {
        }

        /// <summary>
        /// Gets the family.
        /// </summary>
        /// <value>
        /// The family.
        /// </value>
        public FontFamily Family { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => this.instance.Value.Description.FontName;
        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public float Size { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Font"/> is bold.
        /// </summary>
        /// <value>
        ///   <c>true</c> if bold; otherwise, <c>false</c>.
        /// </value>
        public bool Bold => (instance.Value.Description.Style & FontStyle.Bold) == FontStyle.Bold;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Font"/> is italic.
        /// </summary>
        /// <value>
        ///   <c>true</c> if italic; otherwise, <c>false</c>.
        /// </value>
        public bool Italic => (instance.Value.Description.Style & FontStyle.Italic) == FontStyle.Italic;

        /// <summary>
        /// Gets the size of the em.
        /// </summary>
        /// <value>
        /// The size of the em.
        /// </value>
        public ushort EmSize => instance.Value.EmSize;

        /// <summary>
        /// Gets the font instance.
        /// </summary>
        /// <value>
        /// The font instance.
        /// </value>
        internal IFontInstance FontInstance => instance.Value;

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public Glyph GetGlyph(char character)
        {
            return new Glyph(instance.Value.GetGlyph(character), Size);
        }

        private IFontInstance LoadInstanceInternal()
        {
            var instance = Family.Find(requestedStyle);

            if (instance == null && requestedStyle.HasFlag(FontStyle.Italic))
            {
                // can find style requested and they want one thats atleast partial itallic try the regual italic
                instance = Family.Find(FontStyle.Italic);
            }

            if (instance == null && requestedStyle.HasFlag(FontStyle.Bold))
            {
                // can find style requested and they want one thats atleast partial bold try the regular bold
                instance = Family.Find(FontStyle.Bold);
            }

            if (instance == null)
            {
                // cant find style requested and lets just try returning teh default
                instance = Family.Find(Family.DefaultStyle);
            }

            return instance;
        }
    }
}
