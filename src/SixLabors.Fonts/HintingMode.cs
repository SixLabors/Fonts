// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines modes to determine how to apply hinting. The use of mathematical instructions
    /// to adjust the display of an outline font so that it lines up with a rasterized grid.
    /// </summary>
    public enum HintingMode
    {
        /// <summary>
        /// Do not hint the glyphs.
        /// </summary>
        None,

        /// <summary>
        /// Hint the glyphs in a vertical direction only. <see href="http://agg.sourceforge.net/antigrain.com/research/font_rasterization/"/>.
        /// </summary>
        HintY,

        /// <summary>
        /// Hint the glyphs in both directions.
        /// </summary>
        HintXY
    }
}
