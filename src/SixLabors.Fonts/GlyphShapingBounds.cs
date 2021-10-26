// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents the shaped bounds of a glyph.
    /// Uses a class over a struct for ease of use.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class GlyphShapingBounds
    {
        public GlyphShapingBounds(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        private string DebuggerDisplay
            => FormattableString.Invariant($"{this.X} : {this.Y} : {this.Width} : {this.Height}");
    }
}
