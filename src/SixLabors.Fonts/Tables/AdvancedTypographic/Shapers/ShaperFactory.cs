// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    internal static class ShaperFactory
    {
        /// <summary>
        /// Creates a Shaper based on the given script language.
        /// </summary>
        /// <param name="script">The script language.</param>
        /// <returns>A shaper for the given script.</returns>
        public static BaseShaper Create(Script script)
            => script switch
            {
                Script.Arabic or Script.Mongolian or Script.Syriac or Script.Nko or Script.PhagsPa or Script.Mandaic or Script.Manichaean or Script.PsalterPahlavi => new ArabicShaper(),
                _ => new DefaultShaper(),
            };
    }
}
