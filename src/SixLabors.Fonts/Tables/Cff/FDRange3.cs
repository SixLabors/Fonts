// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Represents an element in an FD array.
    /// </summary>
    internal readonly struct FDRange3
    {
        /// <summary>
        /// First glyph index in range
        /// </summary>
        public readonly ushort First;

        /// <summary>
        /// FD index for all glyphs in range
        /// </summary>
        public readonly byte Fd;

        public FDRange3(ushort first, byte fd)
        {
            this.First = first;
            this.Fd = fd;
        }

#if DEBUG
        public override string ToString() => "first:" + this.First + ",fd:" + this.Fd;
#endif
    }
}
