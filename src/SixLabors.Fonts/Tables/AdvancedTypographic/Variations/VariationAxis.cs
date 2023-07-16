// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/otspec184/fvar#variationaxisrecord"/>
    /// </summary>
    [DebuggerDisplay("Name: {Name}, Tag: {Tag}, Min: {Min}, Max: {Max}, Default: {Default}")]
    public struct VariationAxis
    {
        /// <summary>
        /// The name of the axes.
        /// </summary>
        public string Name;

        /// <summary>
        /// Tag identifying the design variation for the axis.
        /// </summary>
        public string Tag;

        /// <summary>
        /// The minimum coordinate value for the axis.
        /// </summary>
        public float Min;

        /// <summary>
        /// The maximum coordinate value for the axis.
        /// </summary>
        public float Max;

        /// <summary>
        /// The default coordinate value for the axis.
        /// </summary>
        public float Default;
    }
}
