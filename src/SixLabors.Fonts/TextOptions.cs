// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides configuration options for rendering and shaping text.
    /// </summary>
    public class TextOptions
    {
        private float dpi = 72F;
        private float lineSpacing = 1F;
        private Font? font;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextOptions"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        public TextOptions(Font font) => this.Font = font;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextOptions"/> class from properties
        /// copied from the given instance.
        /// </summary>
        /// <param name="options">The options whose properties are copied into this instance.</param>
        public TextOptions(TextOptions options)
        {
            this.Font = options.Font;
            this.FallbackFontFamilies = new List<FontFamily>(options.FallbackFontFamilies);
            this.TabWidth = options.TabWidth;
            this.HintingMode = options.HintingMode;
            this.Dpi = options.Dpi;
            this.LineSpacing = options.LineSpacing;
            this.Origin = options.Origin;
            this.WrappingLength = options.WrappingLength;
            this.WordBreaking = options.WordBreaking;
            this.TextDirection = options.TextDirection;
            this.TextAlignment = options.TextAlignment;
            this.TextJustification = options.TextJustification;
            this.HorizontalAlignment = options.HorizontalAlignment;
            this.VerticalAlignment = options.VerticalAlignment;
            this.LayoutMode = options.LayoutMode;
            this.KerningMode = options.KerningMode;
            this.ColorFontSupport = options.ColorFontSupport;
            this.FeatureTags = new List<Tag>(options.FeatureTags);
            this.TextRuns = new List<TextRun>(options.TextRuns);
        }

        /// <summary>
        /// Gets or sets the font.
        /// </summary>
        public Font Font
        {
            get => this.font!;
            set
            {
                Guard.NotNull(value, nameof(this.Font));
                this.font = value;
            }
        }

        /// <summary>
        /// Gets or sets the collection of fallback font families to use when
        /// a specific glyph is missing from <see cref="Font"/>.
        /// </summary>
        public IReadOnlyList<FontFamily> FallbackFontFamilies { get; set; } = Array.Empty<FontFamily>();

        /// <summary>
        /// Gets or sets the DPI (Dots Per Inch) to render/measure the text at.
        /// <para/>
        /// Defaults to 72.
        /// </summary>
        public float Dpi
        {
            get => this.dpi;

            set
            {
                Guard.MustBeGreaterThanOrEqualTo(value, 0, nameof(this.Dpi));
                this.dpi = value;
            }
        }

        /// <summary>
        /// Gets or sets the width of the tab. Measured as the distance in spaces (U+0020).
        /// </summary>
        /// <remarks>
        /// If value is -1 then the font default tab width is used.
        /// </remarks>
        public float TabWidth { get; set; } = -1F;

        /// <summary>
        /// Gets or sets a value indicating whether to apply hinting - The use of mathematical instructions
        /// to adjust the display of an outline font so that it lines up with a rasterized grid.
        /// </summary>
        public HintingMode HintingMode { get; set; }

        /// <summary>
        /// Gets or sets the line spacing. Applied as a multiple of the line height.
        /// <para/>
        /// Defaults to 1.
        /// </summary>
        public float LineSpacing
        {
            get => this.lineSpacing;

            set
            {
                Guard.IsTrue(value != 0, nameof(this.LineSpacing), "Value must not be equal to 0.");
                this.lineSpacing = value;
            }
        }

        /// <summary>
        /// Gets or sets the rendering origin.
        /// </summary>
        public Vector2 Origin { get; set; } = Vector2.Zero;

        /// <summary>
        /// Gets or sets the length in pixel units (px) at which text will automatically wrap onto a new line.
        /// This property also affects the width or height (depending on the <see cref="LayoutMode"/>) of the text box
        /// for alignment of text.
        /// </summary>
        /// <remarks>
        /// If value is -1 then wrapping is disabled.
        /// </remarks>
        public float WrappingLength { get; set; } = -1F;

        /// <summary>
        /// Gets or sets the word breaking mode to use when wrapping text.
        /// </summary>
        public WordBreaking WordBreaking { get; set; }

        /// <summary>
        /// Gets or sets the text direction.
        /// </summary>
        public TextDirection TextDirection { get; set; } = TextDirection.Auto;

        /// <summary>
        /// Gets or sets the text alignment of the text within the box.
        /// </summary>
        public TextAlignment TextAlignment { get; set; }

        /// <summary>
        /// Gets or sets the justification of the text within the box.
        /// </summary>
        public TextJustification TextJustification { get; set; }

        /// <summary>
        /// Gets or sets the horizontal alignment of the text box.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the vertical alignment of the text box.
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the layout mode for the text lines.
        /// </summary>
        public LayoutMode LayoutMode { get; set; }

        /// <summary>
        /// Gets or sets the kerning mode indicating whether to apply kerning (character spacing adjustments)
        /// to the glyph positions from information found within the font.
        /// </summary>
        public KerningMode KerningMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable various color font formats.
        /// </summary>
        public ColorFontSupport ColorFontSupport { get; set; } = ColorFontSupport.MicrosoftColrFormat;

        /// <summary>
        /// Gets or sets the collection of additional feature tags to apply during glyph shaping.
        /// </summary>
        public IReadOnlyList<Tag> FeatureTags { get; set; } = Array.Empty<Tag>();

        /// <summary>
        /// Gets or sets an optional collection of text runs to apply to the body of text.
        /// </summary>
        public IReadOnlyList<TextRun> TextRuns { get; set; } = Array.Empty<TextRun>();
    }
}
