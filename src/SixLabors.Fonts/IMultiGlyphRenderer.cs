using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A surface that support multiple glyphs being rendered onto it sequentially.
    /// </summary>
    /// <seealso cref="SixLabors.Fonts.IGlyphRender" />
    public interface IMultiGlyphRenderer : IGlyphRender
    {
        /// <summary>
        /// Sets the origin.
        /// </summary>
        /// <param name="vector">The vector.</param>
        void SetOrigin(Vector2 vector);
    }
}