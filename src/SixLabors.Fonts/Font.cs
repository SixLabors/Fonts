// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

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
            this.Family = family ?? throw new ArgumentNullException(nameof(family));
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
            : this(prototype?.Family ?? throw new ArgumentNullException(nameof(prototype)), prototype.Size, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        public Font(Font prototype, float size, FontStyle style)
            : this(prototype?.Family ?? throw new ArgumentNullException(nameof(prototype)), size, style)
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
        public bool Bold => (this.instance.Value.Description.Style & FontStyle.Bold) == FontStyle.Bold;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Font"/> is italic.
        /// </summary>
        /// <value>
        ///   <c>true</c> if italic; otherwise, <c>false</c>.
        /// </value>
        public bool Italic => (this.instance.Value.Description.Style & FontStyle.Italic) == FontStyle.Italic;

        /// <summary>
        /// Gets the size of the em.
        /// </summary>
        /// <value>
        /// The size of the em.
        /// </value>
        public ushort EmSize => this.instance.Value.EmSize;

        /// <summary>
        /// Gets the font instance.
        /// </summary>
        /// <value>
        /// The font instance.
        /// </value>
        internal IFontInstance FontInstance => this.instance.Value;

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>Returns the glyph</returns>
        internal Glyph GetGlyph(char character)
        {
            return new Glyph(this.instance.Value.GetGlyph(character), this.Size);
        }

        private IFontInstance LoadInstanceInternal()
        {
            IFontInstance instance = this.Family.Find(this.requestedStyle);

            if (instance == null && this.requestedStyle.HasFlag(FontStyle.Italic))
            {
                // can find style requested and they want one thats atleast partial itallic try the regual italic
                instance = this.Family.Find(FontStyle.Italic);
            }

            if (instance == null && this.requestedStyle.HasFlag(FontStyle.Bold))
            {
                // can find style requested and they want one thats atleast partial bold try the regular bold
                instance = this.Family.Find(FontStyle.Bold);
            }

            if (instance == null)
            {
                // cant find style requested and lets just try returning teh default
                instance = this.Family.Find(this.Family.DefaultStyle);
            }

            return instance;
        }
    }
}
