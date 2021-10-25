// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    internal abstract class BaseShaper
    {
        public MarkZeroingMode MarkZeroingMode { get; protected set; }

        /// <summary>
        /// Assigns the substitution features to each glyph within the collection.
        /// </summary>
        /// <param name="collection">The glyph shaping collection.</param>
        /// <param name="index">The zero-based index of the elements to assign.</param>
        /// <param name="count">The number of elements to assign.</param>
        public abstract void AssignFeatures(IGlyphShapingCollection collection, int index, int count);

        public abstract IEnumerable<Tag> GetShapingStageFeatures();
    }
}
