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
        /// provides a callback to enable custom logic to request decoration details.
        /// a custom TextRun might use alternative triggers to determine what decorations it needs access to.
        /// </summary>
        /// <returns>The text decorations the render wants render info for.</returns>
        public TextDecoration EnabledDecorations();

        public void SetDecoration(TextDecoration textDecoration, Vector2 start, Vector2 end, float thickness);
    }
}
