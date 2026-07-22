// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Provides configuration options for rendering a glyph by id.
/// </summary>
public class GlyphOptions
{
    private float dpi = 72F;
    private Font? font;

    /// <inheritdoc cref="TextOptions.Font"/>
    public required Font Font
    {
        get => this.font!;
        set
        {
            Guard.NotNull(value, nameof(this.Font));
            this.font = value;
        }
    }

    /// <inheritdoc cref="TextOptions.Dpi"/>
    public float Dpi
    {
        get => this.dpi;

        set
        {
            Guard.MustBeGreaterThanOrEqualTo(value, 0, nameof(this.Dpi));
            this.dpi = value;
        }
    }

    /// <inheritdoc cref="TextOptions.HintingMode"/>
    public HintingMode HintingMode { get; set; }

    /// <inheritdoc cref="TextOptions.LayoutMode"/>
    public LayoutMode LayoutMode { get; set; } = LayoutMode.HorizontalTopBottom;

    /// <inheritdoc cref="TextOptions.ColorFontSupport"/>
    public ColorFontSupport ColorFontSupport { get; set; } = ColorFontSupport.None;

    /// <inheritdoc cref="TextOptions.Origin"/>
    public Vector2 Origin { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the visible region in pixel units (px) used when rendering the glyph.
    /// A glyph that falls outside the region is skipped.
    /// </summary>
    /// <remarks>
    /// If value is <see langword="null"/> then culling is disabled.
    /// </remarks>
    public FontRectangle? VisibleBounds { get; set; }

    /// <summary>
    /// Gets or sets which reference line of the glyph's em box is placed at
    /// <see cref="Origin"/> along the block flow axis.
    /// </summary>
    /// <remarks>
    /// Baseline positions derive from the metrics of <see cref="Font"/>. For a single glyph
    /// <see cref="TextBaseline.LineBox"/> anchors the top of the em box, matching text layout.
    /// </remarks>
    public TextBaseline TextBaseline { get; set; }

    /// <inheritdoc cref="TextOptions.BaselineOffset"/>
    public float BaselineOffset { get; set; }

    /// <summary>
    /// Gets or sets the zero-based grapheme cluster index represented by the glyph.
    /// </summary>
    /// <remarks>
    /// This value is passed to glyph renderers through <see cref="Rendering.GlyphRendererParameters.GraphemeIndex"/>.
    /// Callers rendering shaped glyph runs should set this from the shaping cluster so grapheme-aware renderers can
    /// group glyph paths correctly. The default value is suitable for rendering a single isolated glyph.
    /// </remarks>
    public int GraphemeIndex { get; set; }

    /// <summary>
    /// Gets or sets the text attributes applied to the glyph.
    /// </summary>
    public TextAttributes TextAttributes { get; set; } = TextAttributes.None;

    /// <summary>
    /// Gets or sets the text decorations applied to the glyph.
    /// </summary>
    public TextDecorations TextDecorations { get; set; } = TextDecorations.None;

    /// <inheritdoc cref="TextOptions.DecorationPositioningMode"/>
    public DecorationPositioningMode DecorationPositioningMode { get; set; }

    /// <inheritdoc cref="TextOptions.TextDecorationSkipInk"/>
    public TextDecorationSkipInk TextDecorationSkipInk { get; set; } = TextDecorationSkipInk.Auto;

    /// <summary>
    /// Creates the text run associated with the glyph.
    /// </summary>
    /// <returns>The text run associated with the glyph.</returns>
    protected internal virtual TextRun CreateTextRun()
        => new()
        {
            Start = this.GraphemeIndex,
            End = this.GraphemeIndex + 1,
            Font = this.Font,
            TextAttributes = this.TextAttributes,
            TextDecorations = this.TextDecorations
        };

    /// <summary>
    /// Resolves the per-glyph layout mode for these options. Vertical-mixed layouts choose
    /// per code point: upright for glyphs that render vertically, rotated for the rest.
    /// Rendering and measurement share this mapping so their results always agree.
    /// </summary>
    /// <param name="codePoint">The code point represented by the glyph.</param>
    /// <returns>The per-glyph layout mode.</returns>
    internal GlyphLayoutMode GetGlyphLayoutMode(CodePoint codePoint)
    {
        if (this.LayoutMode.IsVertical())
        {
            return GlyphLayoutMode.Vertical;
        }

        if (this.LayoutMode.IsVerticalMixed())
        {
            return AdvancedTypographicUtils.IsVerticalGlyph(codePoint, this.LayoutMode)
                ? GlyphLayoutMode.Vertical
                : GlyphLayoutMode.VerticalRotated;
        }

        return GlyphLayoutMode.Horizontal;
    }
}
