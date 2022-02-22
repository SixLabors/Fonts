// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Represents the anchor coordinates for a given table.
    /// </summary>
    internal readonly struct AnchorXY
    {
        public AnchorXY(short x, short y)
        {
            this.XCoordinate = x;
            this.YCoordinate = y;
        }

        /// <summary>
        /// Gets the horizontal value, in design units.
        /// </summary>
        public short XCoordinate { get; }

        /// <summary>
        /// Gets the vertical value, in design units.
        /// </summary>
        public short YCoordinate { get; }
    }
}
