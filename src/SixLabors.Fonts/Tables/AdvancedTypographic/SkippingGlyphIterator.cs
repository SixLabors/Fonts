// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal struct SkippingGlyphIterator
    {
        private readonly FontMetrics fontMetrics;
        private bool ignoreMarks;
        private bool ignoreBaseGlyphs;
        private bool ignoreLigatures;
        private ushort markAttachmentType;

        public SkippingGlyphIterator(
            FontMetrics fontMetrics,
            IGlyphShapingCollection collection,
            int index,
            LookupFlags lookupFlags)
        {
            this.fontMetrics = fontMetrics;
            this.Collection = collection;
            this.Index = index;
            this.ignoreMarks = (lookupFlags & LookupFlags.IgnoreMarks) != 0;
            this.ignoreBaseGlyphs = (lookupFlags & LookupFlags.IgnoreBaseGlyphs) != 0;
            this.ignoreLigatures = (lookupFlags & LookupFlags.IgnoreLigatures) != 0;
            this.markAttachmentType = (ushort)((int)(lookupFlags & LookupFlags.MarkAttachmentTypeMask) >> 8);
        }

        public IGlyphShapingCollection Collection { get; }

        public int Index { get; set; }

        public int Next()
        {
            this.Move(1);
            return this.Index;
        }

        public int Increment(int count = 1)
        {
            int direction = count < 0 ? -1 : 1;
            count = Math.Abs(count);
            while (count-- > 0)
            {
                this.Move(direction);
            }

            return this.Index;
        }

        public void Reset(int index, LookupFlags lookupFlags)
        {
            this.Index = index;
            this.ignoreMarks = (lookupFlags & LookupFlags.IgnoreMarks) != 0;
            this.ignoreBaseGlyphs = (lookupFlags & LookupFlags.IgnoreBaseGlyphs) != 0;
            this.ignoreLigatures = (lookupFlags & LookupFlags.IgnoreLigatures) != 0;
            this.markAttachmentType = (ushort)((int)(lookupFlags & LookupFlags.MarkAttachmentTypeMask) >> 8);
        }

        private void Move(int direction)
        {
            this.Index += direction;
            while (this.Index >= 0 && this.Index < this.Collection.Count)
            {
                if (!this.ShouldIgnore(this.Index))
                {
                    break;
                }

                this.Index += direction;
            }
        }

        private readonly bool ShouldIgnore(int index)
        {
            GlyphShapingData data = this.Collection[index];
            GlyphShapingClass shapingClass = AdvancedTypographicUtils.GetGlyphShapingClass(this.fontMetrics, data.GlyphId, data);
            return (this.ignoreMarks && shapingClass.IsMark) ||
                (this.ignoreBaseGlyphs && shapingClass.IsBase) ||
                (this.ignoreLigatures && shapingClass.IsLigature) ||
                (this.markAttachmentType > 0 && shapingClass.IsMark && shapingClass.MarkAttachmentType != this.markAttachmentType);
        }
    }
}
