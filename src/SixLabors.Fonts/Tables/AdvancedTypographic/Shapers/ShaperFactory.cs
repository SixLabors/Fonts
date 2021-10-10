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
        public static BaseShaper Create(ScriptClass script)
            => script switch
            {
                ScriptClass.Arabic or ScriptClass.Mongolian or ScriptClass.Syriac or ScriptClass.Nko or ScriptClass.PhagsPa or ScriptClass.Mandaic or ScriptClass.Manichaean or ScriptClass.PsalterPahlavi => new ArabicShaper(),
                _ => new DefaultShaper(),
            };
    }
}
