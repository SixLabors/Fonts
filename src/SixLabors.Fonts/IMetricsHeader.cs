// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines the contract for the metrics header of a font face.
    /// </summary>
    public interface IMetricsHeader
    {
        /// <summary>
        /// Gets the typographic ascender of the face, expressed in font units.
        /// </summary>
        public short Ascender { get; }

        /// <summary>
        /// Gets the typographic descender of the face, expressed in font units.
        /// </summary>
        public short Descender { get; }

        /// <summary>
        /// Gets the typographic line gap of the face, expressed in font units.
        /// This field should be combined with the <see cref="Ascender"/> and <see cref="Descender"/>
        /// values to determine default line spacing.
        /// </summary>
        public short LineGap { get; }

        /// <summary>
        /// Gets the typographic line spacing of the face, expressed in font units.
        /// </summary>
        public short LineHeight { get; }

        /// <summary>
        /// Gets the maximum advance width, in font units, for all glyphs in this face.
        /// </summary>
        public short AdvanceWidthMax { get; }

        /// <summary>
        /// Gets the maximum advance height, in font units, for all glyphs in this
        /// face.This is only relevant for vertical layouts, and is set to <see cref="LineHeight"/> for
        /// fonts that do not provide vertical metrics.
        /// </summary>
        public short AdvanceHeightMax { get; }
    }
}
