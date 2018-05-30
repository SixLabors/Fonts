using SixLabors.Fonts;
using SixLabors.Primitives;

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
        public static (IPathCollection paths, IPathCollection boxes, IPath textBox) GenerateGlyphsWithBox(string text, PointF location, RendererOptions style)
        {
            GlyphBuilder glyphBuilder = new GlyphBuilder(location);

            TextRenderer renderer = new TextRenderer(glyphBuilder);

            renderer.RenderText(text, style);

            return (glyphBuilder.Paths, glyphBuilder.Boxes, glyphBuilder.TextBox);
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting ing withing the FontSpan
        /// </summary>
        /// <param name="text">The text to generate glyphs for</param>
        /// <param name="location">The location</param>
        /// <param name="style">The style and settings to use while rendering the glyphs</param>
        /// <returns></returns>
        public static IPathCollection GenerateGlyphs(string text, PointF location, RendererOptions style)
        {
            return GenerateGlyphsWithBox(text, location, style).paths;
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting ing withing the FontSpan
        /// </summary>
        /// <param name="text">The text to generate glyphs for</param>
        /// <param name="style">The style and settings to use while rendering the glyphs</param>
        /// <returns></returns>
        public static IPathCollection GenerateGlyphs(string text, RendererOptions style)
        {
            return GenerateGlyphs(text, PointF.Empty, style);
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting ing withing the FontSpan
        /// </summary>
        /// <param name="text">The text to generate glyphs for</param>
        /// <param name="style">The style and settings to use while rendering the glyphs</param>
        /// <returns></returns>
        public static (IPathCollection paths, IPathCollection boxes, IPath textBox) GenerateGlyphsWithBox(string text, RendererOptions style)
        {
            return GenerateGlyphsWithBox(text, PointF.Empty, style);
        }

        /// <summary>
        /// Generates the shapes corresponding the glyphs described by the font and with the setting in within the FontSpan along the described path.
        /// </summary>
        /// <param name="text">The text to generate glyphs for</param>
        /// <param name="path">The path to draw the text in relation to</param>
        /// <param name="style">The style and settings to use while rendering the glyphs</param>
        /// <returns></returns>
        public static IPathCollection GenerateGlyphs(string text, IPath path, RendererOptions style)
        {
            PathGlyphBuilder glyphBuilder = new PathGlyphBuilder(path);

            TextRenderer renderer = new TextRenderer(glyphBuilder);

            renderer.RenderText(text, style);

            return glyphBuilder.Paths;
        }
    }
}
