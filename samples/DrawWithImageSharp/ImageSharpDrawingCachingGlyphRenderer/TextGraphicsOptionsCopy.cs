// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    /// <summary>
    /// Options for influencing the drawing functions.
    /// </summary>
    public class TextGraphicsOptionsCopy : IDeepCloneable<TextGraphicsOptionsCopy>
    {
        private float lineSpacing = 1F;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextGraphicsOptionsCopy"/> class.
        /// </summary>
        public TextGraphicsOptionsCopy()
        {
        }

        private TextGraphicsOptionsCopy(TextGraphicsOptionsCopy source)
        {
            this.AlphaCompositionMode = source.AlphaCompositionMode;
            this.Antialias = source.Antialias;
            this.AntialiasSubpixelDepth = source.AntialiasSubpixelDepth;
            this.ApplyKerning = source.ApplyKerning;
            this.BlendPercentage = source.BlendPercentage;
            this.ColorBlendingMode = source.ColorBlendingMode;
            this.DpiX = source.DpiX;
            this.DpiY = source.DpiY;
            this.LineSpacing = source.LineSpacing;
            this.HorizontalAlignment = source.HorizontalAlignment;
            this.TabWidth = source.TabWidth;
            this.WrapTextWidth = source.WrapTextWidth;
            this.VerticalAlignment = source.VerticalAlignment;
            this.FallbackFonts.AddRange(source.FallbackFonts);
        }

        /// <summary>
        /// Gets or sets a value indicating whether antialiasing should be applied.
        /// Defaults to true.
        /// </summary>
        public bool Antialias { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating the number of subpixels to use while rendering with antialiasing enabled.
        /// </summary>
        public int AntialiasSubpixelDepth { get; set; } = 16;

        /// <summary>
        /// Gets or sets a value indicating the blending percentage to apply to the drawing operation.
        /// </summary>
        public float BlendPercentage { get; set; } = 1F;

        /// <summary>
        /// Gets or sets a value indicating the color blending percentage to apply to the drawing operation.
        /// Defaults to <see cref= "PixelColorBlendingMode.Normal" />.
        /// </summary>
        public PixelColorBlendingMode ColorBlendingMode { get; set; } = PixelColorBlendingMode.Normal;

        /// <summary>
        /// Gets or sets a value indicating the color blending percentage to apply to the drawing operation
        /// Defaults to <see cref= "PixelAlphaCompositionMode.SrcOver" />.
        /// </summary>
        public PixelAlphaCompositionMode AlphaCompositionMode { get; set; } = PixelAlphaCompositionMode.SrcOver;

        /// <summary>
        /// Gets or sets a value indicating whether the text should be drawing with kerning enabled.
        /// Defaults to true;
        /// </summary>
        public bool ApplyKerning { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating the number of space widths a tab should lock to.
        /// Defaults to 4.
        /// </summary>
        public float TabWidth { get; set; } = 4F;

        /// <summary>
        /// Gets or sets a value, if greater than 0, indicating the width at which text should wrap.
        /// Defaults to 0.
        /// </summary>
        public float WrapTextWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the DPI (Dots Per Inch) to render text along the X axis.
        /// Defaults to 72.
        /// </summary>
        public float DpiX { get; set; } = 72F;

        /// <summary>
        /// Gets or sets a value indicating the DPI (Dots Per Inch) to render text along the Y axis.
        /// Defaults to 72.
        /// </summary>
        public float DpiY { get; set; } = 72F;

        /// <summary>
        /// Gets or sets the line spacing. Applied as a multiple of the line height.
        /// Defaults to 1.
        /// </summary>
        public float LineSpacing
        {
            get => this.lineSpacing;

            set
            {
                if (value == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(this.LineSpacing));
                }

                this.lineSpacing = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating how to align the text relative to the rendering space.
        /// If <see cref="WrapTextWidth"/> is greater than zero it will align relative to the space
        /// defined by the location and width, if <see cref="WrapTextWidth"/> equals zero, and thus
        /// wrapping disabled, then the alignment is relative to the drawing location.
        /// Defaults to <see cref="HorizontalAlignment.Left"/>.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>
        /// Gets or sets a value indicating how to align the text relative to the rendering space.
        /// Defaults to <see cref="VerticalAlignment.Top"/>.
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

        /// <summary>
        /// Gets the list of fallback font families to apply to the text drawing operation.
        /// Defaults to <see cref="VerticalAlignment.Top"/>.
        /// </summary>
        public List<FontFamily> FallbackFonts { get; } = new List<FontFamily>();

        /// <summary>
        /// Gets or sets a value indicating whether we should render color(emoji) fonts.
        /// Defaults to true.
        /// </summary>
        public bool RenderColorFonts { get; set; } = true;

        /// <summary>
        /// Performs an implicit conversion from <see cref="GraphicsOptions"/> to <see cref="TextGraphicsOptions"/>.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator TextGraphicsOptionsCopy(GraphicsOptions options)
            => new TextGraphicsOptionsCopy()
            {
                Antialias = options.Antialias,
                AntialiasSubpixelDepth = options.AntialiasSubpixelDepth,
                BlendPercentage = options.BlendPercentage,
                ColorBlendingMode = options.ColorBlendingMode,
                AlphaCompositionMode = options.AlphaCompositionMode
            };

        /// <summary>
        /// Performs an explicit conversion from <see cref="TextGraphicsOptions"/> to <see cref="GraphicsOptions"/>.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator GraphicsOptions(TextGraphicsOptionsCopy options)
            => new GraphicsOptions()
            {
                Antialias = options.Antialias,
                AntialiasSubpixelDepth = options.AntialiasSubpixelDepth,
                ColorBlendingMode = options.ColorBlendingMode,
                AlphaCompositionMode = options.AlphaCompositionMode,
                BlendPercentage = options.BlendPercentage
            };

        /// <inheritdoc/>
        public TextGraphicsOptionsCopy DeepClone() => new TextGraphicsOptionsCopy(this);
    }
}
