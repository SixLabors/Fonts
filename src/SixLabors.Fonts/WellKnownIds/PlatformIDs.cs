using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.WellKnownIds
{
    /// <summary>
    /// platforms ids
    /// </summary>
    public enum PlatformIDs : ushort
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
