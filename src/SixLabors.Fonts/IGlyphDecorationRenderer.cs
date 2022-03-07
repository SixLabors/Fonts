// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A surface that can have a glyph rendered to it as a series of actions with the options of providing glyph decoration details.
    /// </summary>
    public interface IGlyphDecorationRenderer : IGlyphRenderer
    {
        /// <summary>
        /// Provides a callback to enable custom logic to request decoration details.
        /// a custom TextRun might use alternative triggers to determine what decorations it needs access to.
        /// </summary>
        /// <returns>The text decorations the render wants render info for.</returns>
        public TextDecorations EnabledDecorations();

        /// <summary>
        /// Provides the positions required for drawing text decorations onto the <see cref="IGlyphRenderer"/>
        /// </summary>
        /// <param name="textDecorations">The type of decoration these details correspond to.</param>
        /// <param name="start">The start position from where to draw the decorations from.</param>
        /// <param name="end">The end position from where to draw the decorations to.</param>
        /// <param name="thickness">The thickness to draw the decoration.</param>
        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness);
    }
}
