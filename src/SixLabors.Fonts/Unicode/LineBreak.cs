// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Information about a potential line break position.
    /// </summary>
    [DebuggerDisplay("{PositionMeasure}/{PositionWrap} @ {Required}")]
    internal readonly struct LineBreak
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineBreak"/> struct.
        /// </summary>
        /// <param name="positionMeasure">The code point index to measure to</param>
        /// <param name="positionWrap">The code point index to actually break the line at</param>
        /// <param name="required">True if this is a required line break; otherwise false</param>
        public LineBreak(int positionMeasure, int positionWrap, bool required = false)
        {
            this.PositionMeasure = positionMeasure;
            this.PositionWrap = positionWrap;
            this.Required = required;
        }

        /// <summary>
        /// Gets the break position, before any trailing whitespace.
        /// This doesn't include trailing whitespace.
        /// </summary>
        public int PositionMeasure { get; }

        /// <summary>
        /// Gets the break position, after any trailing whitespace.
        /// This includes trailing whitespace.
        /// </summary>
        public int PositionWrap { get; }

        /// <summary>
        /// Gets a value indicating whether there should be a forced line break here.
        /// </summary>
        public bool Required { get; }
    }
}
