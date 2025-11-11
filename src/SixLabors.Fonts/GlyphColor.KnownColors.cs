// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <content>
/// Contains static named color values.
/// <see href="https://www.w3.org/TR/css-color-3/"/>
/// </content>
public readonly partial struct GlyphColor
{
    private static readonly Lazy<Dictionary<string, GlyphColor>> NamedGlyphColorsLookupLazy = new(CreateNamedGlyphColorsLookup, true);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F0F8FF.
    /// </summary>
    public static readonly GlyphColor AliceBlue = new(240, 248, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FAEBD7.
    /// </summary>
    public static readonly GlyphColor AntiqueWhite = new(250, 235, 215, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00FFFF.
    /// </summary>
    public static readonly GlyphColor Aqua = new(0, 255, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #7FFFD4.
    /// </summary>
    public static readonly GlyphColor Aquamarine = new(127, 255, 212, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F0FFFF.
    /// </summary>
    public static readonly GlyphColor Azure = new(240, 255, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F5F5DC.
    /// </summary>
    public static readonly GlyphColor Beige = new(245, 245, 220, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFE4C4.
    /// </summary>
    public static readonly GlyphColor Bisque = new(255, 228, 196, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #000000.
    /// </summary>
    public static readonly GlyphColor Black = new(0, 0, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFEBCD.
    /// </summary>
    public static readonly GlyphColor BlanchedAlmond = new(255, 235, 205, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #0000FF.
    /// </summary>
    public static readonly GlyphColor Blue = new(0, 0, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #8A2BE2.
    /// </summary>
    public static readonly GlyphColor BlueViolet = new(138, 43, 226, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #A52A2A.
    /// </summary>
    public static readonly GlyphColor Brown = new(165, 42, 42, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #DEB887.
    /// </summary>
    public static readonly GlyphColor BurlyWood = new(222, 184, 135, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #5F9EA0.
    /// </summary>
    public static readonly GlyphColor CadetBlue = new(95, 158, 160, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #7FFF00.
    /// </summary>
    public static readonly GlyphColor Chartreuse = new(127, 255, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #D2691E.
    /// </summary>
    public static readonly GlyphColor Chocolate = new(210, 105, 30, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF7F50.
    /// </summary>
    public static readonly GlyphColor Coral = new(255, 127, 80, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #6495ED.
    /// </summary>
    public static readonly GlyphColor CornflowerBlue = new(100, 149, 237, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFF8DC.
    /// </summary>
    public static readonly GlyphColor Cornsilk = new(255, 248, 220, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #DC143C.
    /// </summary>
    public static readonly GlyphColor Crimson = new(220, 20, 60, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00FFFF.
    /// </summary>
    public static readonly GlyphColor Cyan = Aqua;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00008B.
    /// </summary>
    public static readonly GlyphColor DarkBlue = new(0, 0, 139, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #008B8B.
    /// </summary>
    public static readonly GlyphColor DarkCyan = new(0, 139, 139, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #B8860B.
    /// </summary>
    public static readonly GlyphColor DarkGoldenrod = new(184, 134, 11, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #A9A9A9.
    /// </summary>
    public static readonly GlyphColor DarkGray = new(169, 169, 169, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #006400.
    /// </summary>
    public static readonly GlyphColor DarkGreen = new(0, 100, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #A9A9A9.
    /// </summary>
    public static readonly GlyphColor DarkGrey = DarkGray;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #BDB76B.
    /// </summary>
    public static readonly GlyphColor DarkKhaki = new(189, 183, 107, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #8B008B.
    /// </summary>
    public static readonly GlyphColor DarkMagenta = new(139, 0, 139, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #556B2F.
    /// </summary>
    public static readonly GlyphColor DarkOliveGreen = new(85, 107, 47, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF8C00.
    /// </summary>
    public static readonly GlyphColor DarkOrange = new(255, 140, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #9932CC.
    /// </summary>
    public static readonly GlyphColor DarkOrchid = new(153, 50, 204, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #8B0000.
    /// </summary>
    public static readonly GlyphColor DarkRed = new(139, 0, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #E9967A.
    /// </summary>
    public static readonly GlyphColor DarkSalmon = new(233, 150, 122, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #8FBC8F.
    /// </summary>
    public static readonly GlyphColor DarkSeaGreen = new(143, 188, 143, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #483D8B.
    /// </summary>
    public static readonly GlyphColor DarkSlateBlue = new(72, 61, 139, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #2F4F4F.
    /// </summary>
    public static readonly GlyphColor DarkSlateGray = new(47, 79, 79, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #2F4F4F.
    /// </summary>
    public static readonly GlyphColor DarkSlateGrey = DarkSlateGray;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00CED1.
    /// </summary>
    public static readonly GlyphColor DarkTurquoise = new(0, 206, 209, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #9400D3.
    /// </summary>
    public static readonly GlyphColor DarkViolet = new(148, 0, 211, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF1493.
    /// </summary>
    public static readonly GlyphColor DeepPink = new(255, 20, 147, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00BFFF.
    /// </summary>
    public static readonly GlyphColor DeepSkyBlue = new(0, 191, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #696969.
    /// </summary>
    public static readonly GlyphColor DimGray = new(105, 105, 105, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #696969.
    /// </summary>
    public static readonly GlyphColor DimGrey = DimGray;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #1E90FF.
    /// </summary>
    public static readonly GlyphColor DodgerBlue = new(30, 144, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #B22222.
    /// </summary>
    public static readonly GlyphColor Firebrick = new(178, 34, 34, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFFAF0.
    /// </summary>
    public static readonly GlyphColor FloralWhite = new(255, 250, 240, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #228B22.
    /// </summary>
    public static readonly GlyphColor ForestGreen = new(34, 139, 34, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF00FF.
    /// </summary>
    public static readonly GlyphColor Fuchsia = new(255, 0, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #DCDCDC.
    /// </summary>
    public static readonly GlyphColor Gainsboro = new(220, 220, 220, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F8F8FF.
    /// </summary>
    public static readonly GlyphColor GhostWhite = new(248, 248, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFD700.
    /// </summary>
    public static readonly GlyphColor Gold = new(255, 215, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #DAA520.
    /// </summary>
    public static readonly GlyphColor Goldenrod = new(218, 165, 32, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #808080.
    /// </summary>
    public static readonly GlyphColor Gray = new(128, 128, 128, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #008000.
    /// </summary>
    public static readonly GlyphColor Green = new(0, 128, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #ADFF2F.
    /// </summary>
    public static readonly GlyphColor GreenYellow = new(173, 255, 47, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #808080.
    /// </summary>
    public static readonly GlyphColor Grey = Gray;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F0FFF0.
    /// </summary>
    public static readonly GlyphColor Honeydew = new(240, 255, 240, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF69B4.
    /// </summary>
    public static readonly GlyphColor HotPink = new(255, 105, 180, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #CD5C5C.
    /// </summary>
    public static readonly GlyphColor IndianRed = new(205, 92, 92, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #4B0082.
    /// </summary>
    public static readonly GlyphColor Indigo = new(75, 0, 130, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFFFF0.
    /// </summary>
    public static readonly GlyphColor Ivory = new(255, 255, 240, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F0E68C.
    /// </summary>
    public static readonly GlyphColor Khaki = new(240, 230, 140, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #E6E6FA.
    /// </summary>
    public static readonly GlyphColor Lavender = new(230, 230, 250, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFF0F5.
    /// </summary>
    public static readonly GlyphColor LavenderBlush = new(255, 240, 245, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #7CFC00.
    /// </summary>
    public static readonly GlyphColor LawnGreen = new(124, 252, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFFACD.
    /// </summary>
    public static readonly GlyphColor LemonChiffon = new(255, 250, 205, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #ADD8E6.
    /// </summary>
    public static readonly GlyphColor LightBlue = new(173, 216, 230, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F08080.
    /// </summary>
    public static readonly GlyphColor LightCoral = new(240, 128, 128, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #E0FFFF.
    /// </summary>
    public static readonly GlyphColor LightCyan = new(224, 255, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FAFAD2.
    /// </summary>
    public static readonly GlyphColor LightGoldenrodYellow = new(250, 250, 210, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #D3D3D3.
    /// </summary>
    public static readonly GlyphColor LightGray = new(211, 211, 211, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #90EE90.
    /// </summary>
    public static readonly GlyphColor LightGreen = new(144, 238, 144, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #D3D3D3.
    /// </summary>
    public static readonly GlyphColor LightGrey = LightGray;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFB6C1.
    /// </summary>
    public static readonly GlyphColor LightPink = new(255, 182, 193, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFA07A.
    /// </summary>
    public static readonly GlyphColor LightSalmon = new(255, 160, 122, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #20B2AA.
    /// </summary>
    public static readonly GlyphColor LightSeaGreen = new(32, 178, 170, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #87CEFA.
    /// </summary>
    public static readonly GlyphColor LightSkyBlue = new(135, 206, 250, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #778899.
    /// </summary>
    public static readonly GlyphColor LightSlateGray = new(119, 136, 153, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #778899.
    /// </summary>
    public static readonly GlyphColor LightSlateGrey = LightSlateGray;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #B0C4DE.
    /// </summary>
    public static readonly GlyphColor LightSteelBlue = new(176, 196, 222, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFFFE0.
    /// </summary>
    public static readonly GlyphColor LightYellow = new(255, 255, 224, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00FF00.
    /// </summary>
    public static readonly GlyphColor Lime = new(0, 255, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #32CD32.
    /// </summary>
    public static readonly GlyphColor LimeGreen = new(50, 205, 50, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FAF0E6.
    /// </summary>
    public static readonly GlyphColor Linen = new(250, 240, 230, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF00FF.
    /// </summary>
    public static readonly GlyphColor Magenta = Fuchsia;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #800000.
    /// </summary>
    public static readonly GlyphColor Maroon = new(128, 0, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #66CDAA.
    /// </summary>
    public static readonly GlyphColor MediumAquamarine = new(102, 205, 170, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #0000CD.
    /// </summary>
    public static readonly GlyphColor MediumBlue = new(0, 0, 205, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #BA55D3.
    /// </summary>
    public static readonly GlyphColor MediumOrchid = new(186, 85, 211, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #9370DB.
    /// </summary>
    public static readonly GlyphColor MediumPurple = new(147, 112, 219, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #3CB371.
    /// </summary>
    public static readonly GlyphColor MediumSeaGreen = new(60, 179, 113, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #7B68EE.
    /// </summary>
    public static readonly GlyphColor MediumSlateBlue = new(123, 104, 238, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00FA9A.
    /// </summary>
    public static readonly GlyphColor MediumSpringGreen = new(0, 250, 154, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #48D1CC.
    /// </summary>
    public static readonly GlyphColor MediumTurquoise = new(72, 209, 204, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #C71585.
    /// </summary>
    public static readonly GlyphColor MediumVioletRed = new(199, 21, 133, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #191970.
    /// </summary>
    public static readonly GlyphColor MidnightBlue = new(25, 25, 112, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F5FFFA.
    /// </summary>
    public static readonly GlyphColor MintCream = new(245, 255, 250, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFE4E1.
    /// </summary>
    public static readonly GlyphColor MistyRose = new(255, 228, 225, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFE4B5.
    /// </summary>
    public static readonly GlyphColor Moccasin = new(255, 228, 181, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFDEAD.
    /// </summary>
    public static readonly GlyphColor NavajoWhite = new(255, 222, 173, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #000080.
    /// </summary>
    public static readonly GlyphColor Navy = new(0, 0, 128, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FDF5E6.
    /// </summary>
    public static readonly GlyphColor OldLace = new(253, 245, 230, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #808000.
    /// </summary>
    public static readonly GlyphColor Olive = new(128, 128, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #6B8E23.
    /// </summary>
    public static readonly GlyphColor OliveDrab = new(107, 142, 35, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFA500.
    /// </summary>
    public static readonly GlyphColor Orange = new(255, 165, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF4500.
    /// </summary>
    public static readonly GlyphColor OrangeRed = new(255, 69, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #DA70D6.
    /// </summary>
    public static readonly GlyphColor Orchid = new(218, 112, 214, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #EEE8AA.
    /// </summary>
    public static readonly GlyphColor PaleGoldenrod = new(238, 232, 170, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #98FB98.
    /// </summary>
    public static readonly GlyphColor PaleGreen = new(152, 251, 152, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #AFEEEE.
    /// </summary>
    public static readonly GlyphColor PaleTurquoise = new(175, 238, 238, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #DB7093.
    /// </summary>
    public static readonly GlyphColor PaleVioletRed = new(219, 112, 147, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFEFD5.
    /// </summary>
    public static readonly GlyphColor PapayaWhip = new(255, 239, 213, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFDAB9.
    /// </summary>
    public static readonly GlyphColor PeachPuff = new(255, 218, 185, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #CD853F.
    /// </summary>
    public static readonly GlyphColor Peru = new(205, 133, 63, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFC0CB.
    /// </summary>
    public static readonly GlyphColor Pink = new(255, 192, 203, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #DDA0DD.
    /// </summary>
    public static readonly GlyphColor Plum = new(221, 160, 221, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #B0E0E6.
    /// </summary>
    public static readonly GlyphColor PowderBlue = new(176, 224, 230, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #800080.
    /// </summary>
    public static readonly GlyphColor Purple = new(128, 0, 128, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #663399.
    /// </summary>
    public static readonly GlyphColor RebeccaPurple = new(102, 51, 153, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF0000.
    /// </summary>
    public static readonly GlyphColor Red = new(255, 0, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #BC8F8F.
    /// </summary>
    public static readonly GlyphColor RosyBrown = new(188, 143, 143, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #4169E1.
    /// </summary>
    public static readonly GlyphColor RoyalBlue = new(65, 105, 225, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #8B4513.
    /// </summary>
    public static readonly GlyphColor SaddleBrown = new(139, 69, 19, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FA8072.
    /// </summary>
    public static readonly GlyphColor Salmon = new(250, 128, 114, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F4A460.
    /// </summary>
    public static readonly GlyphColor SandyBrown = new(244, 164, 96, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #2E8B57.
    /// </summary>
    public static readonly GlyphColor SeaGreen = new(46, 139, 87, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFF5EE.
    /// </summary>
    public static readonly GlyphColor SeaShell = new(255, 245, 238, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #A0522D.
    /// </summary>
    public static readonly GlyphColor Sienna = new(160, 82, 45, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #C0C0C0.
    /// </summary>
    public static readonly GlyphColor Silver = new(192, 192, 192, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #87CEEB.
    /// </summary>
    public static readonly GlyphColor SkyBlue = new(135, 206, 235, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #6A5ACD.
    /// </summary>
    public static readonly GlyphColor SlateBlue = new(106, 90, 205, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #708090.
    /// </summary>
    public static readonly GlyphColor SlateGray = new(112, 128, 144, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #708090.
    /// </summary>
    public static readonly GlyphColor SlateGrey = SlateGray;

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFFAFA.
    /// </summary>
    public static readonly GlyphColor Snow = new(255, 250, 250, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00FF7F.
    /// </summary>
    public static readonly GlyphColor SpringGreen = new(0, 255, 127, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #4682B4.
    /// </summary>
    public static readonly GlyphColor SteelBlue = new(70, 130, 180, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #D2B48C.
    /// </summary>
    public static readonly GlyphColor Tan = new(210, 180, 140, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #008080.
    /// </summary>
    public static readonly GlyphColor Teal = new(0, 128, 128, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #D8BFD8.
    /// </summary>
    public static readonly GlyphColor Thistle = new(216, 191, 216, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FF6347.
    /// </summary>
    public static readonly GlyphColor Tomato = new(255, 99, 71, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #00000000.
    /// </summary>
    public static readonly GlyphColor Transparent = new(0, 0, 0, 0);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #40E0D0.
    /// </summary>
    public static readonly GlyphColor Turquoise = new(64, 224, 208, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #EE82EE.
    /// </summary>
    public static readonly GlyphColor Violet = new(238, 130, 238, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F5DEB3.
    /// </summary>
    public static readonly GlyphColor Wheat = new(245, 222, 179, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFFFFF.
    /// </summary>
    public static readonly GlyphColor White = new(255, 255, 255, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #F5F5F5.
    /// </summary>
    public static readonly GlyphColor WhiteSmoke = new(245, 245, 245, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #FFFF00.
    /// </summary>
    public static readonly GlyphColor Yellow = new(255, 255, 0, 255);

    /// <summary>
    /// Represents a <see paramref="GlyphColor"/> matching the W3C definition that has an hex value of #9ACD32.
    /// </summary>
    public static readonly GlyphColor YellowGreen = new(154, 205, 50, 255);

    private static Dictionary<string, GlyphColor> CreateNamedGlyphColorsLookup()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(AliceBlue), AliceBlue },
            { nameof(AntiqueWhite), AntiqueWhite },
            { nameof(Aqua), Aqua },
            { nameof(Aquamarine), Aquamarine },
            { nameof(Azure), Azure },
            { nameof(Beige), Beige },
            { nameof(Bisque), Bisque },
            { nameof(Black), Black },
            { nameof(BlanchedAlmond), BlanchedAlmond },
            { nameof(Blue), Blue },
            { nameof(BlueViolet), BlueViolet },
            { nameof(Brown), Brown },
            { nameof(BurlyWood), BurlyWood },
            { nameof(CadetBlue), CadetBlue },
            { nameof(Chartreuse), Chartreuse },
            { nameof(Chocolate), Chocolate },
            { nameof(Coral), Coral },
            { nameof(CornflowerBlue), CornflowerBlue },
            { nameof(Cornsilk), Cornsilk },
            { nameof(Crimson), Crimson },
            { nameof(Cyan), Cyan },
            { nameof(DarkBlue), DarkBlue },
            { nameof(DarkCyan), DarkCyan },
            { nameof(DarkGoldenrod), DarkGoldenrod },
            { nameof(DarkGray), DarkGray },
            { nameof(DarkGreen), DarkGreen },
            { nameof(DarkGrey), DarkGrey },
            { nameof(DarkKhaki), DarkKhaki },
            { nameof(DarkMagenta), DarkMagenta },
            { nameof(DarkOliveGreen), DarkOliveGreen },
            { nameof(DarkOrange), DarkOrange },
            { nameof(DarkOrchid), DarkOrchid },
            { nameof(DarkRed), DarkRed },
            { nameof(DarkSalmon), DarkSalmon },
            { nameof(DarkSeaGreen), DarkSeaGreen },
            { nameof(DarkSlateBlue), DarkSlateBlue },
            { nameof(DarkSlateGray), DarkSlateGray },
            { nameof(DarkSlateGrey), DarkSlateGrey },
            { nameof(DarkTurquoise), DarkTurquoise },
            { nameof(DarkViolet), DarkViolet },
            { nameof(DeepPink), DeepPink },
            { nameof(DeepSkyBlue), DeepSkyBlue },
            { nameof(DimGray), DimGray },
            { nameof(DimGrey), DimGrey },
            { nameof(DodgerBlue), DodgerBlue },
            { nameof(Firebrick), Firebrick },
            { nameof(FloralWhite), FloralWhite },
            { nameof(ForestGreen), ForestGreen },
            { nameof(Fuchsia), Fuchsia },
            { nameof(Gainsboro), Gainsboro },
            { nameof(GhostWhite), GhostWhite },
            { nameof(Gold), Gold },
            { nameof(Goldenrod), Goldenrod },
            { nameof(Gray), Gray },
            { nameof(Green), Green },
            { nameof(GreenYellow), GreenYellow },
            { nameof(Grey), Grey },
            { nameof(Honeydew), Honeydew },
            { nameof(HotPink), HotPink },
            { nameof(IndianRed), IndianRed },
            { nameof(Indigo), Indigo },
            { nameof(Ivory), Ivory },
            { nameof(Khaki), Khaki },
            { nameof(Lavender), Lavender },
            { nameof(LavenderBlush), LavenderBlush },
            { nameof(LawnGreen), LawnGreen },
            { nameof(LemonChiffon), LemonChiffon },
            { nameof(LightBlue), LightBlue },
            { nameof(LightCoral), LightCoral },
            { nameof(LightCyan), LightCyan },
            { nameof(LightGoldenrodYellow), LightGoldenrodYellow },
            { nameof(LightGray), LightGray },
            { nameof(LightGreen), LightGreen },
            { nameof(LightGrey), LightGrey },
            { nameof(LightPink), LightPink },
            { nameof(LightSalmon), LightSalmon },
            { nameof(LightSeaGreen), LightSeaGreen },
            { nameof(LightSkyBlue), LightSkyBlue },
            { nameof(LightSlateGray), LightSlateGray },
            { nameof(LightSlateGrey), LightSlateGrey },
            { nameof(LightSteelBlue), LightSteelBlue },
            { nameof(LightYellow), LightYellow },
            { nameof(Lime), Lime },
            { nameof(LimeGreen), LimeGreen },
            { nameof(Linen), Linen },
            { nameof(Magenta), Magenta },
            { nameof(Maroon), Maroon },
            { nameof(MediumAquamarine), MediumAquamarine },
            { nameof(MediumBlue), MediumBlue },
            { nameof(MediumOrchid), MediumOrchid },
            { nameof(MediumPurple), MediumPurple },
            { nameof(MediumSeaGreen), MediumSeaGreen },
            { nameof(MediumSlateBlue), MediumSlateBlue },
            { nameof(MediumSpringGreen), MediumSpringGreen },
            { nameof(MediumTurquoise), MediumTurquoise },
            { nameof(MediumVioletRed), MediumVioletRed },
            { nameof(MidnightBlue), MidnightBlue },
            { nameof(MintCream), MintCream },
            { nameof(MistyRose), MistyRose },
            { nameof(Moccasin), Moccasin },
            { nameof(NavajoWhite), NavajoWhite },
            { nameof(Navy), Navy },
            { nameof(OldLace), OldLace },
            { nameof(Olive), Olive },
            { nameof(OliveDrab), OliveDrab },
            { nameof(Orange), Orange },
            { nameof(OrangeRed), OrangeRed },
            { nameof(Orchid), Orchid },
            { nameof(PaleGoldenrod), PaleGoldenrod },
            { nameof(PaleGreen), PaleGreen },
            { nameof(PaleTurquoise), PaleTurquoise },
            { nameof(PaleVioletRed), PaleVioletRed },
            { nameof(PapayaWhip), PapayaWhip },
            { nameof(PeachPuff), PeachPuff },
            { nameof(Peru), Peru },
            { nameof(Pink), Pink },
            { nameof(Plum), Plum },
            { nameof(PowderBlue), PowderBlue },
            { nameof(Purple), Purple },
            { nameof(RebeccaPurple), RebeccaPurple },
            { nameof(Red), Red },
            { nameof(RosyBrown), RosyBrown },
            { nameof(RoyalBlue), RoyalBlue },
            { nameof(SaddleBrown), SaddleBrown },
            { nameof(Salmon), Salmon },
            { nameof(SandyBrown), SandyBrown },
            { nameof(SeaGreen), SeaGreen },
            { nameof(SeaShell), SeaShell },
            { nameof(Sienna), Sienna },
            { nameof(Silver), Silver },
            { nameof(SkyBlue), SkyBlue },
            { nameof(SlateBlue), SlateBlue },
            { nameof(SlateGray), SlateGray },
            { nameof(SlateGrey), SlateGrey },
            { nameof(Snow), Snow },
            { nameof(SpringGreen), SpringGreen },
            { nameof(SteelBlue), SteelBlue },
            { nameof(Tan), Tan },
            { nameof(Teal), Teal },
            { nameof(Thistle), Thistle },
            { nameof(Tomato), Tomato },
            { nameof(Transparent), Transparent },
            { nameof(Turquoise), Turquoise },
            { nameof(Violet), Violet },
            { nameof(Wheat), Wheat },
            { nameof(White), White },
            { nameof(WhiteSmoke), WhiteSmoke },
            { nameof(Yellow), Yellow },
            { nameof(YellowGreen), YellowGreen }
        };
}
