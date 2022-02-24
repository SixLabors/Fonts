// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a readonly collection of Operating System fonts.
    /// </summary>
    public interface IReadOnlySystemFontCollection : IReadOnlyFontCollection
    {
        /// <summary>
        /// <para>
        /// Gets the collection of Operating System directories that were searched for font families.
        /// </para>
        /// </summary>
        public IEnumerable<string> SearchDirectories { get; }
    }
}
