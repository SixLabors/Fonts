// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    internal static class FontMetricsExtensions
    {
        public static IFontShaper ToFontShaper(this IFontMetrics fontMetrics)
            => fontMetrics switch
            {
                // TODO: I felt a bit dirty writing this but I couldn't think of
                // another way to keep what I need internal.
                FontMetrics metrics => metrics,
                FileFontMetrics fileFontMetrics => fileFontMetrics,
                _ => throw new InvalidCastException(),
            };
    }
}
