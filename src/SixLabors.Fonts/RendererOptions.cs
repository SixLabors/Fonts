// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// The font style to render onto a peice of text.
    /// </summary>
    public sealed class RendererOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RendererOptions"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        public RendererOptions(Font font)
            : this(font, 72, 72)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererOptions"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="dpi">The dpi.</param>
        public RendererOptions(Font font, float dpi)
            : this(font, dpi, dpi)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererOptions"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="dpiX">The X dpi.</param>
        /// <param name="dpiY">The Y dpi.</param>
        public RendererOptions(Font font, float dpiX, float dpiY)
            : this(font, dpiX, dpiY, Vector2.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererOptions"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="origin">The origin location.</param>
        public RendererOptions(Font font, Vector2 origin)
            : this(font, 72, 72, origin)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererOptions"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="dpi">The dpi.</param>
        /// <param name="origin">The origin location.</param>
        public RendererOptions(Font font, float dpi, Vector2 origin)
            : this(font, dpi, dpi, origin)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererOptions"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="dpiX">The X dpi.</param>
        /// <param name="dpiY">The Y dpi.</param>
        /// <param name="origin">The origin location.</param>
        public RendererOptions(Font font, float dpiX, float dpiY, Vector2 origin)
        {
            this.Origin = origin;
            this.Font = font;
            this.DpiX = dpiX;
            this.DpiY = dpiY;
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
        /// Gets or sets the the current X DPI to render/measure the text at.
        /// </summary>
        public float DpiX { get; set; }

        /// <summary>
        /// Gets or sets the the current Ys DPI to render/measure the text at.
        /// </summary>
        public float DpiY { get; set; }

        /// <summary>
        /// Gets or sets the collection of Fallback fontfamiles to try and use when enspecific glyph is missing.
        /// </summary>
        public IEnumerable<FontFamily> FallbackFontFamilies { get; set; } = Array.Empty<FontFamily>();

        /// <summary>
        /// Gets or sets the width relative to the current DPI at which text will automatically wrap onto a newline
        /// </summary>
        /// <value>
        ///     if value is -1 then wrapping is disabled.
        /// </value>
        public float WrappingWidth { get; set; } = -1;

        /// <summary>
        /// Gets or sets the Horizontal alignment of the text.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the Vertical alignment of the text.
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the rendering origin.
        /// </summary>
        public Vector2 Origin { get; set; } = Vector2.Zero;

        /// <summary>
        /// Gets or sets a value indicating whether we enable various color font formats.
        /// </summary>
        public ColorFontSupport ColorFontSupport { get; set; } = ColorFontSupport.None;

        /// <summary>
        /// Gets the style. In derived classes this could switchout to different fonts mid stream
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The Font style that applies to a region of text.
        /// </returns>
        internal AppliedFontStyle GetStyle(int index, int length)
        {
            IFontInstance[] fallbackFontInstances;
            if (this.FallbackFontFamilies == null)
            {
                fallbackFontInstances = Array.Empty<IFontInstance>();
            }
            else
            {
                fallbackFontInstances = this.FallbackFontFamilies.Select(x => new Font(x, this.Font.Size, this.Font.RequestedStyle).Instance).ToArray();
            }

            return new AppliedFontStyle
            {
                Start = 0,
                End = length - 1,
                PointSize = this.Font.Size,
                MainFont = this.Font.Instance,
                FallbackFonts = fallbackFontInstances,
                TabWidth = this.TabWidth,
                ApplyKerning = this.ApplyKerning
            };
        }
    }
}
