// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SixLabors.Fonts
{
    internal class StringComparerHelpers
    {
        public static StringComparer GetCaseInsenativeStringComparer(CultureInfo culture)
        {
#if SUPPORTS_CULTUREINFO_LCID
            return StringComparer.Create(culture, true);
#else
            return StringComparer.OrdinalIgnoreCase;
#endif
        }
    }
}
