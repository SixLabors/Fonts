// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff
{
    internal struct CffGlyphData
    {
        private readonly byte[][] globalSubrBuffers;
        private readonly byte[][] localSubrBuffers;
        private readonly byte[] charStrings;
        private readonly int nominalWidthX;
        private readonly int version;
        private readonly ItemVariationStore? itemVariationStore;

        public CffGlyphData(
            ushort glyphIndex,
            byte[][] globalSubrBuffers,
            byte[][] localSubrBuffers,
            int nominalWidthX,
            byte[] charStrings,
            int version,
            ItemVariationStore? itemVariationStore = null)
        {
            this.GlyphIndex = glyphIndex;
            this.globalSubrBuffers = globalSubrBuffers;
            this.localSubrBuffers = localSubrBuffers;
            this.nominalWidthX = nominalWidthX;
            this.charStrings = charStrings;
            this.version = version;
            this.itemVariationStore = itemVariationStore;

            this.GlyphName = null;
        }

        public ushort GlyphIndex { get; }

        public string? GlyphName { get; set; }

        public Bounds GetBounds(FVarTable? fVar = null, AVarTable? aVar = null)
        {
            using var engine = new CffEvaluationEngine(
                this.charStrings,
                this.globalSubrBuffers,
                this.localSubrBuffers,
                this.nominalWidthX,
                this.version,
                this.itemVariationStore,
                fVar,
                aVar);

            return engine.GetBounds();
        }

        public void RenderTo(IGlyphRenderer renderer, Vector2 scale, Vector2 offset)
        {
            using var engine = new CffEvaluationEngine(
                 this.charStrings,
                 this.globalSubrBuffers,
                 this.localSubrBuffers,
                 this.nominalWidthX,
                 this.version,
                 this.itemVariationStore);

            engine.RenderTo(renderer, scale, offset);
        }
    }
}
