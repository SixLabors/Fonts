// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    [DebuggerDisplay("Name: {Name}, Tag: {Tag}, Min: {Min}, Max: {Max}, Default: {Default}")]
    public struct VariationAxis
    {
        public string Name;

        public string Tag;

        public float Min;

        public float Max;

        public float Default;
    }
}
