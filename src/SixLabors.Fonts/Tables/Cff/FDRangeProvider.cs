// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
    internal struct FDRangeProvider
    {
        // helper class
        private readonly FDRange3[] ranges;
        private ushort currentGlyphIndex;
        private ushort endGlyphIndexMax;
        private FDRange3 currentRange;
        private int currentSelectedRangeIndex;

        public FDRangeProvider(FDRange3[] ranges)
        {
            this.ranges = ranges;
            this.currentGlyphIndex = 0;
            this.currentSelectedRangeIndex = 0;

            if (ranges?.Length > 0)
            {
                this.currentRange = ranges[0];
                this.endGlyphIndexMax = ranges[1].First;
            }
            else
            {
                // empty
                this.currentRange = default;
                this.endGlyphIndexMax = 0;
            }

            this.SelectedFDArray = 0;
        }

        public byte SelectedFDArray { get; private set; }

        public void SetCurrentGlyphIndex(ushort index)
        {
            // find proper range for selected index
            if (index >= this.currentRange.First && index < this.endGlyphIndexMax)
            {
                // ok, in current range
                this.SelectedFDArray = this.currentRange.FontDictionary;
            }
            else
            {
                // move to next range
                this.currentSelectedRangeIndex++;
                this.currentRange = this.ranges[this.currentSelectedRangeIndex];

                this.endGlyphIndexMax = this.ranges[this.currentSelectedRangeIndex + 1].First;
                if (index >= this.currentRange.First && index < this.endGlyphIndexMax)
                {
                    this.SelectedFDArray = this.currentRange.FontDictionary;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            this.currentGlyphIndex = index;
        }
    }
}
