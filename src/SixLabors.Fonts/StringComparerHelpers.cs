// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;

namespace SixLabors.Fonts
{
    internal static class StringComparerHelpers
    {
        public static StringComparer GetCaseInsensitiveStringComparer(CultureInfo culture)
        {
            if (culture != null)
            {
                return StringComparer.Create(culture, true);
            }

            return StringComparer.OrdinalIgnoreCase;
        }
    }
}
