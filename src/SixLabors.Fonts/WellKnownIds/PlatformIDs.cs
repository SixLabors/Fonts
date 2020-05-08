// Copyright (c) Six Labors and contributors.
// Licensed under the GNU Affero General Public License, Version 3.

namespace SixLabors.Fonts.WellKnownIds
{
    /// <summary>
    /// platforms ids
    /// </summary>
    internal enum PlatformIDs : ushort
    {
        /// <summary>
        /// Unicode platform
        /// </summary>
        Unicode = 0,

        /// <summary>
        /// Script manager code
        /// </summary>
        Macintosh = 1,

        /// <summary>
        /// [deprecated] ISO encoding
        /// </summary>
        ISO = 2,

        /// <summary>
        /// Window encoding
        /// </summary>
        Windows = 3,

        /// <summary>
        /// Custom platform
        /// </summary>
        Custom = 4 // Custom  None
    }
}
