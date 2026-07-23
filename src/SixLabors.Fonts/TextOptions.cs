// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

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
        this.FontWeight = options.FontWeight;
        this.FallbackFontFamilies = new List<FontFamily>(options.FallbackFontFamilies);
        this.TabWidth = options.TabWidth;
        this.HintingMode = options.HintingMode;
        this.Dpi = options.Dpi;
        this.LineSpacing = options.LineSpacing;
        this.Origin = options.Origin;
        this.WrappingLength = options.WrappingLength;
        this.VisibleBounds = options.VisibleBounds;
        this.TextBaseline = options.TextBaseline;
        this.BaselineOffset = options.BaselineOffset;
        this.MaxLines = options.MaxLines;
        this.WordBreaking = options.WordBreaking;
        this.TextEllipsis = options.TextEllipsis;
        this.CustomEllipsis = options.CustomEllipsis;
        this.TextHyphenation = options.TextHyphenation;
        this.CustomHyphen = options.CustomHyphen;
        this.TextDirection = options.TextDirection;
        this.TextBidiMode = options.TextBidiMode;
        this.TextInteractionMode = options.TextInteractionMode;
        this.TextAlignment = options.TextAlignment;
        this.TextJustification = options.TextJustification;
        this.HorizontalAlignment = options.HorizontalAlignment;
        this.VerticalAlignment = options.VerticalAlignment;
        this.LayoutMode = options.LayoutMode;
        this.KerningMode = options.KerningMode;
        this.Tracking = options.Tracking;
        this.ColorFontSupport = options.ColorFontSupport;
        this.FeatureTags = new List<Tag>(options.FeatureTags);
        this.TextRuns = new List<TextRun>(options.TextRuns);
        this.DecorationPositioningMode = options.DecorationPositioningMode;
        this.TextDecorationSkipInk = options.TextDecorationSkipInk;
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
    /// Gets or sets the font weight, or <see langword="null"/> to use the weight implied by
    /// <see cref="Font"/>.
    /// </summary>
    public FontWeight? FontWeight { get; set; }

    /// <summary>
    /// Gets or sets the collection of fallback font families to use when
    /// a specific glyph is missing from <see cref="Font"/>.
    /// </summary>
    public IReadOnlyList<FontFamily> FallbackFontFamilies { get; set; } = Array.Empty<FontFamily>();

    /// <summary>
    /// Gets or sets the DPI (Dots Per Inch) to render/measure the text at.
    /// <para/>
    /// Defaults to <c>72F</c>.
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
    /// Defaults to <c>1F</c>.
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
    /// Gets or sets the visible region in pixel units (px) used when rendering text.
    /// Whole lines that fall outside the region are skipped and the remaining lines are
    /// positioned as if all lines had been rendered.
    /// </summary>
    /// <remarks>
    /// If value is <see langword="null"/> then culling is disabled.
    /// </remarks>
    public FontRectangle? VisibleBounds { get; set; }

    /// <summary>
    /// Gets or sets which reference line of the first laid-out line is placed at
    /// <see cref="Origin"/> along the block flow axis.
    /// </summary>
    /// <remarks>
    /// Baseline positions derive from the metrics of <see cref="Font"/>: horizontal layouts
    /// anchor along Y from the alphabetic baseline, vertical layouts along X from the central
    /// column axis. When the value is not <see cref="TextBaseline.LineBox"/> the block
    /// alignment along the flow axis does not apply; additional wrapped lines stack relative
    /// to the anchored first line.
    /// </remarks>
    public TextBaseline TextBaseline { get; set; }

    /// <summary>
    /// Gets or sets an additional shift of the text away from its anchored position, in pixel
    /// units along the block flow axis. Positive values shift toward the text's over side:
    /// upward for horizontal layouts, toward the over column side for vertical layouts, and
    /// away from the line along its normal when the text follows a path.
    /// </summary>
    /// <remarks>
    /// The shift composes with <see cref="TextBaseline"/> and moves rendered glyphs, ink
    /// bounds, and decorations as a unit. The logical advance is unaffected, matching the
    /// CSS and SVG <c>baseline-shift</c> model.
    /// </remarks>
    public float BaselineOffset { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of lines to lay out.
    /// </summary>
    /// <remarks>
    /// If value is -1 then the number of lines is unlimited.
    /// </remarks>
    public int MaxLines { get; set; } = -1;

    /// <summary>
    /// Gets or sets the word breaking mode to use when wrapping text.
    /// </summary>
    public WordBreaking WordBreaking { get; set; }

    /// <summary>
    /// Gets or sets the ellipsis behavior to use when laid-out text is limited to a maximum number of lines.
    /// </summary>
    public TextEllipsis TextEllipsis { get; set; }

    /// <summary>
    /// Gets or sets the ellipsis marker to use when <see cref="TextEllipsis"/> is <c>Custom</c>.
    /// </summary>
    public CodePoint? CustomEllipsis { get; set; }

    /// <summary>
    /// Gets or sets the hyphenation marker behavior to use when text breaks at hyphenation opportunities.
    /// </summary>
    public TextHyphenation TextHyphenation { get; set; }

    /// <summary>
    /// Gets or sets the hyphenation marker to use when <see cref="TextHyphenation"/> is <c>Custom</c>.
    /// </summary>
    public CodePoint? CustomHyphen { get; set; }

    /// <summary>
    /// Gets or sets the text direction.
    /// </summary>
    public TextDirection TextDirection { get; set; } = TextDirection.Auto;

    /// <summary>
    /// Gets or sets how bidirectional text is resolved.
    /// </summary>
    public TextBidiMode TextBidiMode { get; set; }

    /// <summary>
    /// Gets or sets how caret movement and selection model trailing breaking whitespace.
    /// </summary>
    public TextInteractionMode TextInteractionMode { get; set; }

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
    /// Gets or sets the tracking (letter-spacing) value.
    /// Tracking adjusts the spacing between all characters uniformly and is measured in em.
    /// Positive values increase spacing, negative values decrease spacing, and zero applies no adjustment.
    /// </summary>
    public float Tracking { get; set; }

    /// <summary>
    /// Gets or sets the positioning mode used for rendering decorations.
    /// </summary>
    public DecorationPositioningMode DecorationPositioningMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether underline and overline decorations skip over
    /// glyph ink, leaving gaps around descenders and ascenders that cross the decoration.
    /// Defaults to <see cref="TextDecorationSkipInk.Auto"/>.
    /// </summary>
    public TextDecorationSkipInk TextDecorationSkipInk { get; set; } = TextDecorationSkipInk.Auto;

    /// <summary>
    /// Gets or sets the color font support options.
    /// </summary>
    public ColorFontSupport ColorFontSupport { get; set; } = ColorFontSupport.ColrV1 | ColorFontSupport.ColrV0 | ColorFontSupport.Svg;

    /// <summary>
    /// Gets or sets the collection of additional feature tags to apply during glyph shaping.
    /// </summary>
    public IReadOnlyList<Tag> FeatureTags { get; set; } = Array.Empty<Tag>();

    /// <summary>
    /// Gets or sets an optional collection of text runs to apply to the body of text.
    /// </summary>
    public IReadOnlyList<TextRun> TextRuns { get; set; } = Array.Empty<TextRun>();
}
