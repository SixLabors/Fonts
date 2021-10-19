// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts
{
    internal readonly struct GlyphShapingClass
    {
        public GlyphShapingClass(bool isMark, bool isBase, bool isLigature, ushort markAttachmentType)
        {
            this.IsMark = isMark;
            this.IsBase = isBase;
            this.IsLigature = isLigature;
            this.MarkAttachmentType = markAttachmentType;
        }

        public bool IsMark { get; }

        public bool IsBase { get; }

        public bool IsLigature { get; }

        public ushort MarkAttachmentType { get; }
    }
}
