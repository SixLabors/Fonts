// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Text;

namespace DrawWithImageSharp
{
    /// <summary>
    /// A custom glyph builder used to render character and text bounds.
    /// </summary>
    internal class CustomGlyphBuilder : GlyphBuilder
    {
        private readonly List<FontRectangle> glyphBounds = new();

        public CustomGlyphBuilder()
        {
        }

        public CustomGlyphBuilder(Vector2 origin)
            : base(origin)
        {
        }

        /// <summary>
        /// Gets the paths that have been rendered by this.
        /// </summary>
        public IPathCollection Boxes => new PathCollection(this.glyphBounds.Select(x => new RectangularPolygon(x.Location, x.Size)));

        /// <summary>
        /// Gets the paths that have been rendered by this builder.
        /// </summary>
        public IPath TextBox { get; private set; }

        protected override void BeginText(in FontRectangle rect)
        {
            this.TextBox = new RectangularPolygon(rect.Location, rect.Size);
            base.BeginText(rect);
        }

        protected override void BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            this.glyphBounds.Add(bounds);

            base.BeginText(bounds);
        }
    }
}
