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
        private static readonly Vector2 YInverter = new(1, -1);
        private readonly IGlyphRenderer renderer;
        private Vector2 origin;
        private Vector2 scale;
        private Vector2 offset;
        private Matrix3x2 transform;

        public TransformingGlyphRenderer(IGlyphRenderer renderer, Vector2 origin, Vector2 scale, Vector2 offset, Matrix3x2 transform)
        {
            this.renderer = renderer;
            this.origin = origin;
            this.scale = scale;
            this.offset = offset;
            this.transform = transform;
            this.IsOpen = false;
        }

        public bool IsOpen { get; set; }

        public void BeginFigure()
        {
            this.IsOpen = true;
            this.renderer.BeginFigure();
        }

        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            this.IsOpen = false;
            return this.renderer.BeginGlyph(in bounds, in parameters);
        }

        public void BeginText(in FontRectangle bounds)
        {
            this.IsOpen = false;
            this.renderer.BeginText(in bounds);
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

            this.BeginFigure();
            this.renderer.MoveTo(this.Transform(point));
            this.IsOpen = true;
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

        public TextDecorations EnabledDecorations()
            => this.renderer.EnabledDecorations();

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
            => this.renderer.SetDecoration(textDecorations, this.Transform(start), this.Transform(end), thickness);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 Transform(Vector2 point)
            => (Vector2.Transform((point * this.scale) + this.offset, this.transform) * YInverter) + this.origin;
    }
}
