// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Single-byte Type 2 charstring operators (byte values 0-31).
/// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5177.Type2.pdf"/>
/// </summary>
internal enum Type2Operator1 : byte
{
    /// <summary>
    /// Reserved (0).
    /// </summary>
    Reserved0_ = 0,

    /// <summary>
    /// Horizontal stem hint (1).
    /// </summary>
    Hstem,

    /// <summary>
    /// Reserved (2).
    /// </summary>
    Reserved2_,

    /// <summary>
    /// Vertical stem hint (3).
    /// </summary>
    Vstem,

    /// <summary>
    /// Vertical moveto (4).
    /// </summary>
    Vmoveto,

    /// <summary>
    /// Relative lineto (5).
    /// </summary>
    Rlineto,

    /// <summary>
    /// Horizontal lineto (6).
    /// </summary>
    Hlineto,

    /// <summary>
    /// Vertical lineto (7).
    /// </summary>
    Vlineto,

    /// <summary>
    /// Relative rcurveto (8). Draws cubic Bezier curves.
    /// </summary>
    Rrcurveto,

    /// <summary>
    /// Reserved (9).
    /// </summary>
    Reserved9_,

    /// <summary>
    /// Call local subroutine (10).
    /// </summary>
    Callsubr,

    /// <summary>
    /// Return from subroutine (11).
    /// </summary>
    Return,

    /// <summary>
    /// Escape byte prefix for two-byte operators (12).
    /// </summary>
    Escape,

    /// <summary>
    /// Reserved (13).
    /// </summary>
    Reserved13_,

    /// <summary>
    /// End character (14). Finishes a charstring outline.
    /// </summary>
    Endchar,

    /// <summary>
    /// CFF2 variation store index selector (15).
    /// </summary>
    VsIndex,

    /// <summary>
    /// CFF2 blend operator for font variations (16).
    /// </summary>
    Blend,

    /// <summary>
    /// Reserved (17).
    /// </summary>
    Reserved17_,

    /// <summary>
    /// Horizontal stem hint with hintmask support (18).
    /// </summary>
    Hstemhm,

    /// <summary>
    /// Hint mask (19). Specifies which stem hints are active.
    /// </summary>
    Hintmask,

    /// <summary>
    /// Counter mask (20). Specifies counter control hints.
    /// </summary>
    Cntrmask,

    /// <summary>
    /// Relative moveto (21). Starts a new subpath.
    /// </summary>
    Rmoveto,

    /// <summary>
    /// Horizontal moveto (22). Starts a new subpath.
    /// </summary>
    Hmoveto,

    /// <summary>
    /// Vertical stem hint with hintmask support (23).
    /// </summary>
    Vstemhm,

    /// <summary>
    /// Relative curveto followed by lineto (24).
    /// </summary>
    Rcurveline,

    /// <summary>
    /// Relative lineto followed by curveto (25).
    /// </summary>
    Rlinecurve,

    /// <summary>
    /// Vertical-vertical curveto (26). Draws curves with vertical tangents.
    /// </summary>
    Vvcurveto,

    /// <summary>
    /// Horizontal-horizontal curveto (27). Draws curves with horizontal tangents.
    /// </summary>
    Hhcurveto,

    /// <summary>
    /// Short integer operand (28). Pushes a 16-bit integer onto the stack.
    /// </summary>
    Shortint,

    /// <summary>
    /// Call global subroutine (29).
    /// </summary>
    Callgsubr,

    /// <summary>
    /// Alternating vertical-horizontal curveto (30).
    /// </summary>
    Vhcurveto,

    /// <summary>
    /// Alternating horizontal-vertical curveto (31).
    /// </summary>
    Hvcurveto,
}
