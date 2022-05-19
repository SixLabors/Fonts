// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal struct SkippingGlyphIterator
    {
        private readonly FontMetrics fontMetrics;
        private bool ignoreMarks;
        private bool ignoreBaseGlypghs;
        private bool ignoreLigatures;
        private ushort markAttachmentType;

        public SkippingGlyphIterator(FontMetrics fontMetrics, IGlyphShapingCollection collection, ushort index, LookupFlags lookupFlags)
        {
            this.fontMetrics = fontMetrics;
            this.Collection = collection;
            this.Index = index;
            this.ignoreMarks = (lookupFlags & LookupFlags.IgnoreMarks) != 0;
            this.ignoreBaseGlypghs = (lookupFlags & LookupFlags.IgnoreBaseGlypghs) != 0;
            this.ignoreLigatures = (lookupFlags & LookupFlags.IgnoreLigatures) != 0;
            this.markAttachmentType = 0; // TODO: Lookup HarfBuzz to see how this is assigned.
        }

        public IGlyphShapingCollection Collection { get; }

        public ushort Index { get; set; }

        public ushort Next()
        {
            this.Move(1);
            return this.Index;
        }

        public ushort Increment(int count = 1)
        {
            int direction = count < 0 ? -1 : 1;
            count = Math.Abs(count);
            while (count-- > 0)
            {
                this.Move(direction);
            }

            return this.Index;
        }

        public void Reset(ushort index, LookupFlags lookupFlags)
        {
            this.Index = index;
            this.ignoreMarks = (lookupFlags & LookupFlags.IgnoreMarks) != 0;
            this.ignoreBaseGlypghs = (lookupFlags & LookupFlags.IgnoreBaseGlypghs) != 0;
            this.ignoreLigatures = (lookupFlags & LookupFlags.IgnoreLigatures) != 0;
            this.markAttachmentType = 0; // TODO: Lookup HarfBuzz
        }

        private void Move(int direction)
        {
            this.Index = (ushort)(this.Index + direction);
            while (this.Index >= 0 && this.Index < this.Collection.Count)
            {
                if (!this.ShouldIgnore(this.Index))
                {
                    break;
                }

                this.Index = (ushort)(this.Index + direction);
            }
        }

        private bool ShouldIgnore(int index)
        {
            GlyphShapingData data = this.Collection.GetGlyphShapingData(index);
            GlyphShapingClass shapingClass = AdvancedTypographicUtils.GetGlyphShapingClass(this.fontMetrics, data.GlyphId, data);
            return this.ignoreMarks && shapingClass.IsMark ||
                this.ignoreBaseGlypghs && shapingClass.IsBase ||
                this.ignoreLigatures && shapingClass.IsLigature ||
                this.markAttachmentType > 0 && shapingClass.IsMark && shapingClass.MarkAttachmentType != this.markAttachmentType;
        }
    }
}
