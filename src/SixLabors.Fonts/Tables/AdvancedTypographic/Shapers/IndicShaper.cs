// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// The IndicShaper supports Indic scripts e.g. Devanagari, Kannada, etc.
    /// </summary>
    internal sealed class IndicShaper : DefaultShaper
    {
        private static readonly StateMachine StateMachine =
            new(IndicShapingData.StateTable, IndicShapingData.AcceptingStates, IndicShapingData.Tags);

        public IndicShaper(TextOptions textOptions)
            : base(MarkZeroingMode.None, textOptions)
        {
        }
    }
}
