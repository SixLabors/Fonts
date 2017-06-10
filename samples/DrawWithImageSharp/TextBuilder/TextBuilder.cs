using SixLabors.Fonts;
using SixLabors.Primitives;
using SixLabors.Shapes.Temp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SixLabors.Shapes.Temp
{
    /// <summary>
    /// Text drawing extensions for a PathBuilder
    /// </summary>
    public static class TextBuilder
    {
        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting ing withing the FontSpan
        /// </summary>
        /// <param name="text">The text to generate glyphs for</param>
        /// <param name="location">The location</param>
        /// <param name="style">The style and settings to use while rendering the glyphs</param>
        /// <returns></returns>
        public static IPathCollection GenerateGlyphs(string text, PointF location, FontSpan style)
        {
            var glyphBuilder = new GlyphBuilder(location);

            TextRenderer renderer = new TextRenderer(glyphBuilder);

            renderer.RenderText(text, style);

            return glyphBuilder.Paths;
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting ing withing the FontSpan
        /// </summary>
        /// <param name="text">The text to generate glyphs for</param>
        /// <param name="style">The style and settings to use while rendering the glyphs</param>
        /// <returns></returns>
        public static IPathCollection GenerateGlyphs(string text, FontSpan style)
        {
            return GenerateGlyphs(text, PointF.Empty, style);
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting in within the FontSpan along the described path.
        /// </summary>
        /// <param name="text">The text to generate glyphs for</param>
        /// <param name="path">The path to draw the text in relation to</param>
        /// <param name="style">The style and settings to use while rendering the glyphs</param>
        /// <returns></returns>
        public static IPathCollection GenerateGlyphs(string text, IPath path, FontSpan style)
        {
            var glyphBuilder = new PathGlyphBuilder(path);

            TextRenderer renderer = new TextRenderer(glyphBuilder);

            renderer.RenderText(text, style);

            return glyphBuilder.Paths;
        }
    }
}
