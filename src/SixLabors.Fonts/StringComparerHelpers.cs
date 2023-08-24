// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

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
