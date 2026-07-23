// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Specifies which reference line of laid-out text is placed at <see cref="TextOptions.Origin"/>
/// along the block flow axis, and equivalently which line rides the path when text follows one.
/// Values match the CSS <c>dominant-baseline</c> and HTML canvas <c>textBaseline</c> vocabulary.
/// </summary>
public enum TextBaseline
{
    /// <summary>
    /// The line box anchors at the origin and block alignment positions it along the flow axis.
    /// </summary>
    LineBox = 0,

    /// <summary>
    /// The top of the em box is placed at the origin. Matches CSS <c>text-top</c>.
    /// </summary>
    TextTop,

    /// <summary>
    /// The hanging baseline, from which Tibetan and similar scripts hang, is placed at the
    /// origin. Matches CSS and SVG <c>hanging</c>. Positioned by the font's baseline table
    /// when the font provides one and derived from the ascender otherwise.
    /// </summary>
    Hanging,

    /// <summary>
    /// The middle baseline, half the x-height above the alphabetic baseline, is placed at the
    /// origin. Matches CSS and SVG <c>middle</c>.
    /// </summary>
    Middle,

    /// <summary>
    /// The central baseline, the middle of the em box, is placed at the origin.
    /// The conventional anchor for vertical CJK layout; matches CSS and SVG <c>central</c>.
    /// </summary>
    Central,

    /// <summary>
    /// The alphabetic baseline, the line Latin glyphs sit on, is placed at the origin.
    /// Matches CSS and SVG <c>alphabetic</c> and the HTML canvas default.
    /// </summary>
    Alphabetic,

    /// <summary>
    /// The ideographic-under baseline, beneath CJK ideographs, is placed at the origin.
    /// Matches CSS and SVG <c>ideographic</c>. Positioned by the font's baseline table
    /// when the font provides one and derived from the descender otherwise.
    /// </summary>
    Ideographic,

    /// <summary>
    /// The bottom of the em box is placed at the origin. Matches CSS <c>text-bottom</c>.
    /// </summary>
    TextBottom,
}
