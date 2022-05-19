// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.Cff
{
    internal sealed class CffBoundsFinder : IGlyphRenderer
    {
        private float minX;
        private float maxX;
        private float minY;
        private float maxY;
        private Vector2 currentXY;
        private readonly int nsteps = 3;
        private bool contourOpen;
        private bool firstEval = true;

        public void BeginFigure() => throw new NotSupportedException();

        public bool BeginGlyph(FontRectangle bounds, GlyphRendererParameters parameters)
            => true; // Do nothing.

        public void BeginText(FontRectangle bounds)
            => throw new NotSupportedException();

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
            float eachstep = 1F / this.nsteps;
            float t = eachstep; // Start

            for (int n = 1; n < this.nsteps; ++n)
            {
                float c = 1F - t;
                Vector2 xy = (this.currentXY * c * c * c) + (secondControlPoint * 3 * t * c * c) + (thirdControlPoint * 3 * t * t * c) + (point * t * t * t);
                this.UpdateMinMax(xy.X, xy.Y);

                t += eachstep;
            }

            this.currentXY = point;
            this.UpdateMinMax(point.X, point.Y);
            this.contourOpen = true;
        }

        public void EndFigure()
        {
            this.contourOpen = false;
            this.currentXY = Vector2.Zero;
        }

        public void EndGlyph()
        {
            // Do nothing.
        }

        public void EndText() => throw new NotSupportedException();

        public void LineTo(Vector2 point)
        {
            this.currentXY = point;
            this.UpdateMinMax(point.X, point.Y);
            this.contourOpen = true;
        }

        public void MoveTo(Vector2 point)
        {
            if (this.contourOpen)
            {
                this.EndFigure();
            }

            this.currentXY = point;
            this.UpdateMinMax(point.X, point.Y);
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
            => throw new NotSupportedException();

        private void UpdateMinMax(float x0, float y0)
        {
            if (this.firstEval)
            {
                // 4 times
                if (x0 < this.minX)
                {
                    this.minX = x0;
                }

                if (x0 > this.maxX)
                {
                    this.maxX = x0;
                }

                if (y0 < this.minY)
                {
                    this.minY = y0;
                }

                if (y0 > this.maxY)
                {
                    this.maxY = y0;
                }

                this.firstEval = false;
            }
            else
            {
                // 2 times
                if (x0 < this.minX)
                {
                    this.minX = x0;
                }
                else if (x0 > this.maxX)
                {
                    this.maxX = x0;
                }

                if (y0 < this.minY)
                {
                    this.minY = y0;
                }
                else if (y0 > this.maxY)
                {
                    this.maxY = y0;
                }
            }
        }

        public Bounds GetBounds()
            => new(
                (short)Math.Floor(this.minX),
                (short)Math.Floor(this.minY),
                (short)Math.Ceiling(this.maxX),
                (short)Math.Ceiling(this.maxY));
    }
}
