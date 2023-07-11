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

            // Variations tables are only present for CFF2 format.
            this.FVar = null;
            this.AVar = null;
            this.GVar = null;
        }

        public ushort GlyphIndex { get; }

        public string? GlyphName { get; set; }

        public FVarTable? FVar { get; set; }

        public AVarTable? AVar { get; set; }

        public GVarTable? GVar { get; set; }

        public Bounds GetBounds()
        {
            using var engine = new CffEvaluationEngine(
                this.charStrings,
                this.globalSubrBuffers,
                this.localSubrBuffers,
                this.nominalWidthX,
                this.version,
                this.itemVariationStore,
                this.FVar,
                this.AVar);

            return engine.GetBounds();
        }

        public void RenderTo(IGlyphRenderer renderer, Vector2 origin, Vector2 scale, Vector2 offset, Matrix3x2 transform)
        {
            using var engine = new CffEvaluationEngine(
                 this.charStrings,
                 this.globalSubrBuffers,
                 this.localSubrBuffers,
                 this.nominalWidthX,
                 this.version,
                 this.itemVariationStore,
                 this.FVar,
                 this.AVar);

            engine.RenderTo(renderer, origin, scale, offset, transform);
        }
    }
}
