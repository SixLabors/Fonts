using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    [Flags]
    public enum FontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        BoldItalic = 3,
        // not yet supported
        //Underline = 4,
        //Strikeout = 8
    }
}
