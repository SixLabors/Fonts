// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a particular format for text, including font face, size, and style attributes. This class cannot be inherited.
    /// </summary>
    public sealed class Font
    {
        private readonly Lazy<IFontInstance?> instance;
        private readonly Lazy<string> fontName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        public Font(FontFamily family, float size, FontStyle style)
        {
            this.Family = family ?? throw new ArgumentNullException(nameof(family));
            this.RequestedStyle = style;
            this.Size = size;
            this.instance = new Lazy<IFontInstance?>(this.LoadInstanceInternal);
            this.fontName = new Lazy<string>(this.LoadFontName);
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
            : this(prototype.Family, size, prototype.RequestedStyle)
        {
        }

        /// <summary>
        /// Gets the family.
        /// </summary>
        /// <value>
        /// The family.
        /// </value>
        internal FontStyle RequestedStyle { get; }

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
        public string Name => this.fontName.Value;

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
        public bool Bold => (this.Instance.Description.Style & FontStyle.Bold) == FontStyle.Bold;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Font"/> is italic.
        /// </summary>
        /// <value>
        ///   <c>true</c> if italic; otherwise, <c>false</c>.
        /// </value>
        public bool Italic => (this.Instance.Description.Style & FontStyle.Italic) == FontStyle.Italic;

        /// <summary>
        /// Gets the size of the em.
        /// </summary>
        /// <value>
        /// The size of the em.
        /// </value>
        public ushort EmSize => this.Instance.EmSize;

        /// <summary>
        /// Gets the ascender (from the OS/2 table field <c>TypoAscender</c>).
        /// </summary>
        public short Ascender => this.Instance.Ascender;

        /// <summary>
        /// Gets the descender (from the OS/2 table field <c>TypoDescender</c>).
        /// </summary>
        public short Descender => this.Instance.Descender;

        /// <summary>
        /// Gets the line gap (from the OS/2 table field <c>TypoLineGap</c>).
        /// </summary>
        public short LineGap => this.Instance.LineGap;

        /// <summary>
        /// Gets the line height.
        /// </summary>
        public int LineHeight => this.Instance.LineHeight;

        /// <summary>
        /// Gets the font instance.
        /// </summary>
        /// <value>
        /// The font instance.
        /// </value>
        public IFontInstance Instance => this.instance.Value ?? throw new FontException("Font instance not found");

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="codePoint">The code point of the character.</param>
        /// <returns>Returns the glyph</returns>
        public Glyph GetGlyph(int codePoint)
        {
            return new Glyph(this.Instance.GetGlyph(codePoint), this.Size);
        }

        private string LoadFontName()
        {
            return this.instance.Value?.Description.FontName(this.Family.Culture) ?? string.Empty;
        }

        private IFontInstance? LoadInstanceInternal()
        {
            IFontInstance? instance = this.Family.Find(this.RequestedStyle);

            if (instance is null && this.RequestedStyle.HasFlag(FontStyle.Italic))
            {
                // can find style requested and they want one thats atleast partial itallic try the regual italic
                instance = this.Family.Find(FontStyle.Italic);
            }

            if (instance is null && this.RequestedStyle.HasFlag(FontStyle.Bold))
            {
                // can find style requested and they want one thats atleast partial bold try the regular bold
                instance = this.Family.Find(FontStyle.Bold);
            }

            if (instance is null)
            {
                // cant find style requested and lets just try returning teh default
                instance = this.Family.Find(this.Family.DefaultStyle);
            }

            return instance;
        }
    }
}
