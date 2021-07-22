// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a particular format for text, including font face, size, and style attributes. This class cannot be inherited.
    /// </summary>
    public sealed class Font
    {
        private readonly Lazy<IFontMetrics?> metrics;
        private readonly Lazy<string> fontName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="family">The font family.</param>
        /// <param name="size">The size of the font in PT units.</param>
        /// <param name="style">The font style.</param>
        public Font(FontFamily family, float size, FontStyle style)
        {
            this.Family = family ?? throw new ArgumentNullException(nameof(family));
            this.RequestedStyle = style;
            this.Size = size;
            this.metrics = new Lazy<IFontMetrics?>(this.LoadInstanceInternal);
            this.fontName = new Lazy<string>(this.LoadFontName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="family">The font family.</param>
        /// <param name="size">The size of the font in PT units.</param>
        public Font(FontFamily family, float size)
            : this(family, size, FontStyle.Regular)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="style">The font style.</param>
        public Font(Font prototype, FontStyle style)
            : this(prototype?.Family ?? throw new ArgumentNullException(nameof(prototype)), prototype.Size, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="size">The size of the font in PT units.</param>
        /// <param name="style">The font style.</param>
        public Font(Font prototype, float size, FontStyle style)
            : this(prototype?.Family ?? throw new ArgumentNullException(nameof(prototype)), size, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Font"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="size">The size of the font in PT units.</param>
        public Font(Font prototype, float size)
            : this(prototype.Family, size, prototype.RequestedStyle)
        {
        }

        /// <summary>
        /// Gets the family.
        /// </summary>
        public FontFamily Family { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name => this.fontName.Value;

        /// <summary>
        /// Gets the size of the font in PT units.
        /// </summary>
        public float Size { get; }

        /// <inheritdoc cref="IFontMetrics.UnitsPerEm"/>
        public ushort UnitsPerEm => this.FontMetrics.UnitsPerEm;

        /// <inheritdoc cref="IFontMetrics.Ascender"/>
        public short Ascender => this.FontMetrics.Ascender;

        /// <inheritdoc cref="IFontMetrics.Descender"/>
        public short Descender => this.FontMetrics.Descender;

        /// <inheritdoc cref="IFontMetrics.LineGap"/>
        public short LineGap => this.FontMetrics.LineGap;

        /// <inheritdoc cref="IFontMetrics.LineHeight"/>
        public int LineHeight => this.FontMetrics.LineHeight;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Font"/> is bold.
        /// </summary>
        public bool Bold => (this.FontMetrics.Description.Style & FontStyle.Bold) == FontStyle.Bold;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Font"/> is italic.
        /// </summary>
        public bool Italic => (this.FontMetrics.Description.Style & FontStyle.Italic) == FontStyle.Italic;

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        public IFontMetrics FontMetrics => this.metrics.Value ?? throw new FontException("Font instance not found.");

        /// <summary>
        /// Gets the requested style.
        /// </summary>
        internal FontStyle RequestedStyle { get; }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="codePoint">The code point of the character.</param>
        /// <returns>Returns the glyph</returns>
        public Glyph GetGlyph(CodePoint codePoint) => new Glyph(this.FontMetrics.GetGlyphMetrics(codePoint), this.Size);

        private string LoadFontName() => this.metrics.Value?.Description.FontName(this.Family.Culture) ?? string.Empty;

        private IFontMetrics? LoadInstanceInternal()
        {
            IFontMetrics? instance = this.Family.Find(this.RequestedStyle);

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
