// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// This shaper is an implementation of the Universal Shaping Engine, which
    /// uses Unicode data to shape a number of scripts without a dedicated shaping engine.
    /// <see href="https://www.microsoft.com/typography/OpenTypeDev/USE/intro.htm"/>.
    /// </summary>
    internal class UniversalShaper : DefaultShaper
    {
        public UniversalShaper(TextOptions textOptions)
            : base(MarkZeroingMode.PostGpos, textOptions)
        {
        }

        // TODO: Implement. I'm stuck on the state table generation.
    }
}
