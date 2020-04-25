// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Options for enabling color font support during layout and rendering.
    /// </summary>
    [Flags] // flags is because in the future we might want to add support for additional color font storage formats and might want to support multiple at once.
    public enum ColorFontSupport
    {
        /// <summary>
        /// Don't try rendering color glyphs at all
        /// </summary>
        None = 0,

        /// <summary>
        /// Render using glyphs accessed via Micropsfts COLR/CPAL table extensions to OpenType
        /// </summary>
        MicrosoftColrFormat = 1
    }
}
