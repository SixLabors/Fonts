// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Used to apply a transform against any glyphs rendered by the engine.
    /// </summary>
    internal struct TransformingGlyphRenderer : IGlyphRenderer
    {
        private Vector2 scale;
        private Vector2 offset;
        private readonly IGlyphRenderer renderer;

        public TransformingGlyphRenderer(Vector2 scale, Vector2 offset, IGlyphRenderer renderer)
        {
            this.scale = scale;
            this.offset = offset;
            this.renderer = renderer;
            this.IsOpen = false;
        }

        public bool IsOpen { get; set; }

        public void BeginFigure()
        {
            this.IsOpen = false;
            this.renderer.BeginFigure();
        }

        public bool BeginGlyph(FontRectangle bounds, GlyphRendererParameters parameters)
        {
            this.IsOpen = false;
            return this.renderer.BeginGlyph(bounds, parameters);
        }

        public void BeginText(FontRectangle bounds)
        {
            this.IsOpen = false;
            this.renderer.BeginText(bounds);
        }

        public void EndFigure()
        {
            this.IsOpen = false;
            this.renderer.EndFigure();
        }

        public void EndGlyph()
        {
            this.IsOpen = false;
            this.renderer.EndGlyph();
        }

        public void EndText()
        {
            this.IsOpen = false;
            this.renderer.EndText();
        }

        public void LineTo(Vector2 point)
        {
            this.IsOpen = true;
            this.renderer.LineTo(this.Transform(point));
        }

        public void MoveTo(Vector2 point)
        {
            if (this.IsOpen)
            {
                this.EndFigure();
            }

            this.renderer.MoveTo(this.Transform(point));
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            this.IsOpen = true;
            this.renderer.CubicBezierTo(this.Transform(secondControlPoint), this.Transform(thirdControlPoint), this.Transform(point));
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
            this.IsOpen = true;
            this.renderer.QuadraticBezierTo(this.Transform(secondControlPoint), this.Transform(point));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 Transform(Vector2 point) => (point * this.scale) + this.offset;
    }
}
