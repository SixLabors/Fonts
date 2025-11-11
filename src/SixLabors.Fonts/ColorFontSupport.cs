// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies which color font formats are enabled for layout and rendering.
/// </summary>
/// <remarks>
/// This enumeration allows a renderer to select which OpenType color font
/// technologies to honor when processing glyph runs. Multiple formats may be
/// enabled simultaneously.
/// </remarks>
[Flags]
public enum ColorFontSupport
{
    /// <summary>
    /// Disable color font rendering entirely. All glyphs will be drawn as monochrome outlines.
    /// </summary>
    None = 0,

    /// <summary>
    /// Enable rendering of COLR version 0 color glyphs (layered solid colors defined by COLR/CPAL tables).
    /// </summary>
    ColrV0 = 1,

    /// <summary>
    /// Enable rendering of COLR version 1 color glyphs (paint graph-based color glyphs with gradients and transforms).
    /// </summary>
    ColrV1 = 2,

    /// <summary>
    /// Enable rendering of color glyphs stored as SVG documents in the OpenType SVG table.
    /// </summary>
    Svg = 4
}
