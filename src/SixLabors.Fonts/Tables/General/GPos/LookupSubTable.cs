// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace SixLabors.Fonts.Tables.General.GPos
{
    internal abstract class LookupSubTable
    {
        protected LookupSubTable(GPosTable owner) => this.Owner = owner;

        public GPosTable Owner { get; }

        public abstract void SetGlyphPosition(int offset, int length);
    }
}
