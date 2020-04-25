// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    /// <summary>
    /// Using the brush as a source of pixels colors blends the brush color with source.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal class DrawTextProcessorCopy<TPixel> : ImageProcessor<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        private BaseCachingGlyphRenderer textRenderer;

        private readonly DrawTextProcessorCopy definition;

        public DrawTextProcessorCopy(Configuration configuration, DrawTextProcessorCopy definition, Image<TPixel> source, Rectangle sourceRectangle)
            : base(configuration, source, sourceRectangle)
        {
            this.definition = definition;
        }

        private TextGraphicsOptionsCopy Options => this.definition.Options;

        private Font Font => this.definition.Font;

        private PointF Location => this.definition.Location;

        private string Text => this.definition.Text;

        private IPen Pen => this.definition.Pen;

        private SolidBrushCopy Brush => this.definition.Brush;

        protected override void BeforeImageApply()
        {
            base.BeforeImageApply();

            // do everything at the image level as we are delegating the processing down to other processors
            var style = new RendererOptions(this.Font, this.Options.DpiX, this.Options.DpiY, this.Location)
            {
                ApplyKerning = this.Options.ApplyKerning,
                TabWidth = this.Options.TabWidth,
                WrappingWidth = this.Options.WrapTextWidth,
                HorizontalAlignment = this.Options.HorizontalAlignment,
                VerticalAlignment = this.Options.VerticalAlignment,
                FallbackFontFamilies = this.Options.FallbackFonts,
                ColorFontSupport = this.definition.Options.RenderColorFonts ? ColorFontSupport.MicrosoftColrFormat : ColorFontSupport.None,
            };

            // (this.definition.Options.RenderColorFonts)
            //{
                this.textRenderer = new ColorCachingGlyphRenderer(this.Configuration.MemoryAllocator, this.Text.Length, this.Pen, this.Brush != null);
           // }
           // else
           // {
           //     this.textRenderer = new CachingGlyphRenderer(this.Configuration.MemoryAllocator, this.Text.Length, this.Pen, this.Brush != null);
           // }

            this.textRenderer.Options = (GraphicsOptions)this.Options;
            var renderer = new TextRenderer(this.textRenderer);
            renderer.RenderText(this.Text, style);
        }

        protected override void AfterImageApply()
        {
            base.AfterImageApply();
            this.textRenderer?.Dispose();
            this.textRenderer = null;
        }

        /// <inheritdoc/>
        protected override void OnFrameApply(ImageFrame<TPixel> source)
        {
            // this is a no-op as we have processes all as an image, we should be able to pass out of before email apply a skip frames outcome
            Draw(this.textRenderer.FillOperations, this.Brush);
            //Draw(this.textRenderer.OutlineOperations, this.Pen?.StrokeFill);

            void Draw(List<DrawingOperation> operations, SolidBrushCopy brush)
            {
                if (operations?.Count > 0)
                {
                    var brushes = new Dictionary<Color, BrushApplicatorCopy<TPixel>>();
                    foreach (DrawingOperation operation in operations)
                    {
                        if (operation.Color.HasValue)
                        {
                            if (!brushes.TryGetValue(operation.Color.Value, out _))
                            {
                                brushes[operation.Color.Value] = new SolidBrushCopy(operation.Color.Value).CreateApplicator(this.Configuration, this.textRenderer.Options, source, this.SourceRectangle);
                            }
                        }
                    }

                    using (BrushApplicatorCopy<TPixel> app = brush.CreateApplicator(this.Configuration, this.textRenderer.Options, source, this.SourceRectangle))
                    {
                        foreach (DrawingOperation operation in operations)
                        {
                            var currentApp = app;
                            if (operation.Color != null)
                            {
                                brushes.TryGetValue(operation.Color.Value, out currentApp);
                            }

                            Buffer2D<float> buffer = operation.Map;
                            int startY = operation.Location.Y;
                            int startX = operation.Location.X;
                            int offsetSpan = 0;

                            if (startX + buffer.Height < 0)
                            {
                                continue;
                            }

                            if (startX + buffer.Width < 0)
                            {
                                continue;
                            }

                            if (startX < 0)
                            {
                                offsetSpan = -startX;
                                startX = 0;
                            }

                            if (startX >= source.Width)
                            {
                                continue;
                            }

                            int firstRow = 0;
                            if (startY < 0)
                            {
                                firstRow = -startY;
                            }

                            int maxHeight = source.Height - startY;
                            int end = Math.Min(operation.Map.Height, maxHeight);

                            for (int row = firstRow; row < end; row++)
                            {
                                int y = startY + row;
                                Span<float> span = buffer.GetRowSpan(row).Slice(offsetSpan);
                                currentApp.Apply(span, startX, y);
                            }
                        }
                    }

                    foreach (var app in brushes.Values)
                    {
                        app.Dispose();
                    }
                }
            }
        }

        private struct DrawingOperation
        {
            public Buffer2D<float> Map { get; set; }

            public Point Location { get; set; }

            public Color? Color { get; set; }
        }

        private sealed class ColorCachingGlyphRenderer : BaseCachingGlyphRenderer, IColorGlyphRenderer
        {
            public ColorCachingGlyphRenderer(MemoryAllocator memoryAllocator, int size, IPen pen, bool renderFill)
                : base(memoryAllocator, size, pen, renderFill)
            {
            }

            public void SetColor(GlyphColor color)
            {
                this.SetLayerColor(new Color(new Rgba32(color.Red, color.Green, color.Blue, color.Alpha)));
            }
        }

        private sealed class CachingGlyphRenderer : BaseCachingGlyphRenderer
        {
            public CachingGlyphRenderer(MemoryAllocator memoryAllocator, int size, IPen pen, bool renderFill)
                : base(memoryAllocator, size, pen, renderFill)
            {
            }
        }

        private abstract class BaseCachingGlyphRenderer : IGlyphRenderer, IDisposable
        {
            // just enough accuracy to allow for 1/8 pixel differences which
            // later are accumulated while rendering, but do not grow into full pixel offsets
            // The value 8 is benchmarked to:
            // - Provide a good accuracy (smaller than 0.2% image difference compared to the non-caching variant)
            // - Cache hit ratio above 60%
            private const float AccuracyMultiple = 8;

            private readonly PathBuilder builder;

            private Point currentRenderPosition;
            private (GlyphRendererParameters glyph, PointF subPixelOffset) currentGlyphRenderParams;
            private readonly int offset;
            private PointF currentPoint;
            private Color? currentColor;

            private readonly Dictionary<(GlyphRendererParameters glyph, PointF subPixelOffset), GlyphRenderData>
                glyphData = new Dictionary<(GlyphRendererParameters glyph, PointF subPixelOffset), GlyphRenderData>();

            private readonly bool renderOutline;
            private readonly bool renderFill;
            private bool rasterizationRequired;

            public BaseCachingGlyphRenderer(MemoryAllocator memoryAllocator, int size, IPen pen, bool renderFill)
            {
                this.MemoryAllocator = memoryAllocator;
                this.currentRenderPosition = default;
                this.Pen = pen;
                this.renderFill = renderFill;
                this.renderOutline = pen != null;
                this.offset = 2;
                if (this.renderFill)
                {
                    this.FillOperations = new List<DrawingOperation>(size);
                }

                if (this.renderOutline)
                {
                    this.offset = (int)MathF.Ceiling((pen.StrokeWidth * 2) + 2);
                    this.OutlineOperations = new List<DrawingOperation>(size);
                }

                this.builder = new PathBuilder();
            }

            protected void SetLayerColor(Color color)
            {
                this.currentColor = color;
            }

            public List<DrawingOperation> FillOperations { get; }

            public List<DrawingOperation> OutlineOperations { get; }

            public MemoryAllocator MemoryAllocator { get; internal set; }

            public IPen Pen { get; internal set; }

            public GraphicsOptions Options { get; internal set; }

            public void BeginFigure()
            {
                this.builder.StartFigure();
            }

            public bool BeginGlyph(FontRectangle bounds, GlyphRendererParameters parameters)
            {
                this.currentColor = null;
                this.currentRenderPosition = Point.Truncate(bounds.Location);
                PointF subPixelOffset = bounds.Location - this.currentRenderPosition;

                subPixelOffset.X = MathF.Round(subPixelOffset.X * AccuracyMultiple) / AccuracyMultiple;
                subPixelOffset.Y = MathF.Round(subPixelOffset.Y * AccuracyMultiple) / AccuracyMultiple;

                // we have offset our rendering origin a little bit down to prevent edge cropping, move the draw origin up to compensate
                this.currentRenderPosition = new Point(this.currentRenderPosition.X - this.offset, this.currentRenderPosition.Y - this.offset);
                this.currentGlyphRenderParams = (parameters, subPixelOffset);

                if (this.glyphData.ContainsKey(this.currentGlyphRenderParams))
                {
                    // we have already drawn the glyph vectors skip trying again
                    this.rasterizationRequired = false;
                    return false;
                }

                // we check to see if we have a render cache and if we do then we render else
                this.builder.Clear();

                // ensure all glyphs render around [zero, zero]  so offset negative root positions so when we draw the glyph we can offset it back
                this.builder.SetOrigin(new PointF(-(int)bounds.X + this.offset, -(int)bounds.Y + this.offset));

                this.rasterizationRequired = true;
                return true;
            }

            public void BeginText(FontRectangle bounds)
            {
                // not concerned about this one
                this.OutlineOperations?.Clear();
                this.FillOperations?.Clear();
            }

            public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
            {
                this.builder.AddBezier(this.currentPoint, secondControlPoint, thirdControlPoint, point);
                this.currentPoint = point;
            }

            public void Dispose()
            {
                foreach (KeyValuePair<(GlyphRendererParameters glyph, PointF subPixelOffset), GlyphRenderData> kv in this.glyphData)
                {
                    kv.Value.Dispose();
                }

                this.glyphData.Clear();
            }

            public void EndFigure()
            {
                this.builder.CloseFigure();
            }

            public void EndGlyph()
            {
                GlyphRenderData renderData = default;

                // has the glyph been rendered already?
                if (this.rasterizationRequired)
                {
                    IPath path = this.builder.Build();

                    // if we are using the fonts color layers we ignore the request to draw an outline only
                    // cause that wont really work and instead force drawing with fill with the requested color
                    // if color fonts disabled then this.currentColor will always be null
                    if (this.renderFill || this.currentColor != null)
                    {
                        renderData.FillMap = this.Render(path);
                        renderData.Color = this.currentColor;
                    }

                    if (this.renderOutline && this.currentColor == null)
                    {
                        if (this.Pen.StrokePattern.Length == 0)
                        {
                            path = path.GenerateOutline(this.Pen.StrokeWidth);
                        }
                        else
                        {
                            path = path.GenerateOutline(this.Pen.StrokeWidth, this.Pen.StrokePattern);
                        }

                        renderData.OutlineMap = this.Render(path);
                    }

                    this.glyphData[this.currentGlyphRenderParams] = renderData;
                }
                else
                {
                    renderData = this.glyphData[this.currentGlyphRenderParams];
                }

                if (renderData.FillMap != null)
                {
                    this.FillOperations.Add(new DrawingOperation
                    {
                        Location = this.currentRenderPosition,
                        Map = renderData.FillMap,
                        Color = renderData.Color
                    });
                }

                if (renderData.OutlineMap != null)
                {
                    this.OutlineOperations.Add(new DrawingOperation
                    {
                        Location = this.currentRenderPosition,
                        Map = renderData.OutlineMap
                    });
                }
            }

            private Buffer2D<float> Render(IPath path)
            {
                Size size = Rectangle.Ceiling(path.Bounds).Size;
                size = new Size(size.Width + (this.offset * 2), size.Height + (this.offset * 2));

                float subpixelCount = 4;
                float offset = 0.5f;
                if (this.Options.Antialias)
                {
                    offset = 0f; // we are antialiasing skip offsetting as real antialiasing should take care of offset.
                    subpixelCount = this.Options.AntialiasSubpixelDepth;
                    if (subpixelCount < 4)
                    {
                        subpixelCount = 4;
                    }
                }

                // take the path inside the path builder, scan thing and generate a Buffer2d representing the glyph and cache it.
                Buffer2D<float> fullBuffer = this.MemoryAllocator.Allocate2D<float>(size.Width + 1, size.Height + 1, AllocationOptions.Clean);

                using (IMemoryOwner<float> bufferBacking = this.MemoryAllocator.Allocate<float>(path.MaxIntersections))
                using (IMemoryOwner<PointF> rowIntersectionBuffer = this.MemoryAllocator.Allocate<PointF>(size.Width))
                {
                    float subpixelFraction = 1f / subpixelCount;
                    float subpixelFractionPoint = subpixelFraction / subpixelCount;
                    Span<PointF> intersectionSpan = rowIntersectionBuffer.Memory.Span;
                    Span<float> buffer = bufferBacking.Memory.Span;

                    for (int y = 0; y <= size.Height; y++)
                    {
                        Span<float> scanline = fullBuffer.GetRowSpan(y);
                        bool scanlineDirty = false;
                        float yPlusOne = y + 1;

                        for (float subPixel = y; subPixel < yPlusOne; subPixel += subpixelFraction)
                        {
                            var start = new PointF(path.Bounds.Left - 1, subPixel);
                            var end = new PointF(path.Bounds.Right + 1, subPixel);
                            int pointsFound = path.FindIntersections(start, end, intersectionSpan, IntersectionRule.Nonzero);

                            if (pointsFound == 0)
                            {
                                // nothing on this line skip
                                continue;
                            }

                            for (int i = 0; i < pointsFound && i < intersectionSpan.Length; i++)
                            {
                                buffer[i] = intersectionSpan[i].X;
                            }

                            QuickSort.Sort(buffer.Slice(0, pointsFound));

                            for (int point = 0; point < pointsFound; point += 2)
                            {
                                // points will be paired up
                                float scanStart = buffer[point];
                                float scanEnd = buffer[point + 1];
                                int startX = (int)MathF.Floor(scanStart + offset);
                                int endX = (int)MathF.Floor(scanEnd + offset);

                                if (startX >= 0 && startX < scanline.Length)
                                {
                                    for (float x = scanStart; x < startX + 1; x += subpixelFraction)
                                    {
                                        scanline[startX] += subpixelFractionPoint;
                                        scanlineDirty = true;
                                    }
                                }

                                if (endX >= 0 && endX < scanline.Length)
                                {
                                    for (float x = endX; x < scanEnd; x += subpixelFraction)
                                    {
                                        scanline[endX] += subpixelFractionPoint;
                                        scanlineDirty = true;
                                    }
                                }

                                int nextX = startX + 1;
                                endX = Math.Min(endX, scanline.Length); // reduce to end to the right edge
                                nextX = Math.Max(nextX, 0);
                                for (int x = nextX; x < endX; x++)
                                {
                                    scanline[x] += subpixelFraction;
                                    scanlineDirty = true;
                                }
                            }
                        }

                        if (scanlineDirty)
                        {
                            if (!this.Options.Antialias)
                            {
                                for (int x = 0; x < size.Width; x++)
                                {
                                    if (scanline[x] >= 0.5)
                                    {
                                        scanline[x] = 1;
                                    }
                                    else
                                    {
                                        scanline[x] = 0;
                                    }
                                }
                            }
                        }
                    }
                }

                return fullBuffer;
            }

            public void EndText()
            {
            }

            public void LineTo(Vector2 point)
            {
                this.builder.AddLine(this.currentPoint, point);
                this.currentPoint = point;
            }

            public void MoveTo(Vector2 point)
            {
                this.builder.StartFigure();
                this.currentPoint = point;
            }

            public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
            {
                this.builder.AddBezier(this.currentPoint, secondControlPoint, point);
                this.currentPoint = point;
            }

            private struct GlyphRenderData : IDisposable
            {
                public Color? Color;

                public Buffer2D<float> FillMap;

                public PointF FillOffset;

                public Buffer2D<float> OutlineMap;

                internal PointF OutlineOffset;

                public void Dispose()
                {
                    this.FillMap?.Dispose();
                    this.OutlineMap?.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Provides an implementation of a solid brush for painting solid color areas.
    /// </summary>
    public class SolidBrushCopy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolidBrush"/> class.
        /// </summary>
        /// <param name="color">The color.</param>
        public SolidBrushCopy(Color color)
        {
            this.Color = color;
        }

        /// <summary>
        /// Gets the color.
        /// </summary>
        public Color Color { get; }

        /// <inheritdoc />
        public BrushApplicatorCopy<TPixel> CreateApplicator<TPixel>(
            Configuration configuration,
            GraphicsOptions options,
            ImageFrame<TPixel> source,
            RectangleF region)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            return new SolidBrushApplicatorCopy<TPixel>(configuration, options, source, this.Color.ToPixel<TPixel>());
        }
    }
    /// <summary>
    /// The solid brush applicator.
    /// </summary>
    public class SolidBrushApplicatorCopy<TPixel> : BrushApplicatorCopy<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidBrushApplicator{TPixel}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration instance to use when performing operations.</param>
        /// <param name="options">The graphics options.</param>
        /// <param name="source">The source image.</param>
        /// <param name="color">The color.</param>
        public SolidBrushApplicatorCopy(
            Configuration configuration,
            GraphicsOptions options,
            ImageFrame<TPixel> source,
            TPixel color)
            : base(configuration, options, source)
        {
            this.Colors = configuration.MemoryAllocator.Allocate<TPixel>(source.Width);
            this.Colors.Memory.Span.Fill(color);
        }

        /// <summary>
        /// Gets the colors.
        /// </summary>
        protected IMemoryOwner<TPixel> Colors { get; private set; }

        /// <inheritdoc/>
        internal override TPixel this[int x, int y] => this.Colors.Memory.Span[x];

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing)
            {
                this.Colors.Dispose();
            }

            this.Colors = null;
            this.isDisposed = true;
        }

        /// <inheritdoc />
        internal override void Apply(Span<float> scanline, int x, int y)
        {
            Span<TPixel> destinationRow = this.Target.GetPixelRowSpan(y).Slice(x);

            // constrain the spans to each other
            if (destinationRow.Length > scanline.Length)
            {
                destinationRow = destinationRow.Slice(0, scanline.Length);
            }
            else
            {
                scanline = scanline.Slice(0, destinationRow.Length);
            }

            Configuration configuration = this.Configuration;
            MemoryAllocator memoryAllocator = configuration.MemoryAllocator;

            if (this.Options.BlendPercentage == 1f)
            {
                this.Blender.Blend(configuration, destinationRow, destinationRow, this.Colors.Memory.Span, scanline);
            }
            else
            {
                using (IMemoryOwner<float> amountBuffer = memoryAllocator.Allocate<float>(scanline.Length))
                {
                    Span<float> amountSpan = amountBuffer.Memory.Span;

                    for (int i = 0; i < scanline.Length; i++)
                    {
                        amountSpan[i] = scanline[i] * this.Options.BlendPercentage;
                    }

                    this.Blender.Blend(
                        configuration,
                        destinationRow,
                        destinationRow,
                        this.Colors.Memory.Span,
                        amountSpan);
                }
            }
        }
    }

    /// <summary>
    /// A primitive that converts a point into a color for discovering the fill color based on an implementation.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    /// <seealso cref="IDisposable" />
    public abstract class BrushApplicatorCopy<TPixel> : IDisposable
        where TPixel : unmanaged, IPixel<TPixel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BrushApplicator{TPixel}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration instance to use when performing operations.</param>
        /// <param name="options">The graphics options.</param>
        /// <param name="target">The target.</param>
        internal BrushApplicatorCopy(Configuration configuration, GraphicsOptions options, ImageFrame<TPixel> target)
        {
            this.Configuration = configuration;
            this.Target = target;
            this.Options = options;
            this.Blender = PixelOperations<TPixel>.Instance.GetPixelBlender(options);
        }

        /// <summary>
        /// Gets the configuration instance to use when performing operations.
        /// </summary>
        protected Configuration Configuration { get; }

        /// <summary>
        /// Gets the pixel blender.
        /// </summary>
        internal PixelBlender<TPixel> Blender { get; }

        /// <summary>
        /// Gets the target image.
        /// </summary>
        protected ImageFrame<TPixel> Target { get; }

        /// <summary>
        /// Gets thegraphics options
        /// </summary>
        protected GraphicsOptions Options { get; }

        /// <summary>
        /// Gets the overlay pixel at the specified position.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <returns>The <see typeparam="TPixel"/> at the specified position.</returns>
        internal abstract TPixel this[int x, int y] { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the object and frees resources for the Garbage Collector.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed and unmanaged objects.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Applies the opacity weighting for each pixel in a scanline to the target based on the pattern contained in the brush.
        /// </summary>
        /// <param name="scanline">A collection of opacity values between 0 and 1 to be merged with the brushed color value before being applied to the target.</param>
        /// <param name="x">The x-position in the target pixel space that the start of the scanline data corresponds to.</param>
        /// <param name="y">The y-position in  the target pixel space that whole scanline corresponds to.</param>
        /// <remarks>scanlineBuffer will be > scanlineWidth but provide and offset in case we want to share a larger buffer across runs.</remarks>
        internal virtual void Apply(Span<float> scanline, int x, int y)
        {
            MemoryAllocator memoryAllocator = this.Configuration.MemoryAllocator;

            using (IMemoryOwner<float> amountBuffer = memoryAllocator.Allocate<float>(scanline.Length))
            using (IMemoryOwner<TPixel> overlay = memoryAllocator.Allocate<TPixel>(scanline.Length))
            {
                Span<float> amountSpan = amountBuffer.Memory.Span;
                Span<TPixel> overlaySpan = overlay.Memory.Span;
                float blendPercentage = this.Options.BlendPercentage;

                if (blendPercentage < 1)
                {
                    for (int i = 0; i < scanline.Length; i++)
                    {
                        amountSpan[i] = scanline[i] * blendPercentage;
                        overlaySpan[i] = this[x + i, y];
                    }
                }
                else
                {
                    for (int i = 0; i < scanline.Length; i++)
                    {
                        amountSpan[i] = scanline[i];
                        overlaySpan[i] = this[x + i, y];
                    }
                }

                Span<TPixel> destinationRow = this.Target.GetPixelRowSpan(y).Slice(x, scanline.Length);
                this.Blender.Blend(this.Configuration, destinationRow, destinationRow, overlaySpan, amountSpan);
            }
        }
    }
    /// <summary>
    /// Optimized quick sort implementation for Span{float} input
    /// </summary>
    internal static class QuickSort
    {
        /// <summary>
        /// Sorts the elements of <paramref name="data"/> in ascending order
        /// </summary>
        /// <param name="data">The items to sort</param>
        public static void Sort(Span<float> data)
        {
            if (data.Length < 2)
            {
                return;
            }

            if (data.Length == 2)
            {
                if (data[0] > data[1])
                {
                    Swap(ref data[0], ref data[1]);
                }

                return;
            }

            Sort(ref data[0], 0, data.Length - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref float left, ref float right)
        {
            float tmp = left;
            left = right;
            right = tmp;
        }

        private static void Sort(ref float data0, int lo, int hi)
        {
            if (lo < hi)
            {
                int p = Partition(ref data0, lo, hi);
                Sort(ref data0, lo, p);
                Sort(ref data0, p + 1, hi);
            }
        }

        private static int Partition(ref float data0, int lo, int hi)
        {
            float pivot = Unsafe.Add(ref data0, lo);
            int i = lo - 1;
            int j = hi + 1;
            while (true)
            {
                do
                {
                    i = i + 1;
                }
                while (Unsafe.Add(ref data0, i) < pivot && i < hi);

                do
                {
                    j = j - 1;
                }
                while (Unsafe.Add(ref data0, j) > pivot && j > lo);

                if (i >= j)
                {
                    return j;
                }

                Swap(ref Unsafe.Add(ref data0, i), ref Unsafe.Add(ref data0, j));
            }
        }

        /// <summary>
        /// Sorts the elements of <paramref name="data"/> in ascending order
        /// </summary>
        /// <param name="sortable">The items to sort on</param>
        /// <param name="data">The items to sort</param>
        public static void Sort<T>(Span<float> sortable, Span<T> data)
        {
            if (sortable.Length != data.Length)
            {
                throw new Exception("both spans must be the same length");
            }

            if (sortable.Length < 2)
            {
                return;
            }

            if (sortable.Length == 2)
            {
                if (sortable[0] > sortable[1])
                {
                    Swap(ref sortable[0], ref sortable[1]);
                    Swap(ref data[0], ref data[1]);
                }

                return;
            }

            Sort(ref sortable[0], 0, sortable.Length - 1, ref data[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap<T>(ref T left, ref T right)
        {
            T tmp = left;
            left = right;
            right = tmp;
        }

        private static void Sort<T>(ref float data0, int lo, int hi, ref T dataToSort)
        {
            if (lo < hi)
            {
                int p = Partition(ref data0, lo, hi, ref dataToSort);
                Sort(ref data0, lo, p, ref dataToSort);
                Sort(ref data0, p + 1, hi, ref dataToSort);
            }
        }

        private static int Partition<T>(ref float data0, int lo, int hi, ref T dataToSort)
        {
            float pivot = Unsafe.Add(ref data0, lo);
            int i = lo - 1;
            int j = hi + 1;
            while (true)
            {
                do
                {
                    i = i + 1;
                }
                while (Unsafe.Add(ref data0, i) < pivot && i < hi);

                do
                {
                    j = j - 1;
                }
                while (Unsafe.Add(ref data0, j) > pivot && j > lo);

                if (i >= j)
                {
                    return j;
                }

                Swap(ref Unsafe.Add(ref data0, i), ref Unsafe.Add(ref data0, j));
                Swap(ref Unsafe.Add(ref dataToSort, i), ref Unsafe.Add(ref dataToSort, j));
            }
        }

        /// <summary>
        /// Sorts the elements of <paramref name="sortable"/> in ascending order, and swapping items in <paramref name="data1"/> and <paramref name="data2"/> in sequance with them.
        /// </summary>
        /// <param name="sortable">The items to sort on</param>
        /// <param name="data1">The set of items to sort</param>
        /// <param name="data2">The 2nd set of items to sort</param>
        public static void Sort<T1, T2>(Span<float> sortable, Span<T1> data1, Span<T2> data2)
        {
            if (sortable.Length != data1.Length)
            {
                throw new Exception("both spans must be the same length");
            }

            if (sortable.Length != data2.Length)
            {
                throw new Exception("both spans must be the same length");
            }

            if (sortable.Length < 2)
            {
                return;
            }

            if (sortable.Length == 2)
            {
                if (sortable[0] > sortable[1])
                {
                    Swap(ref sortable[0], ref sortable[1]);
                    Swap(ref data1[0], ref data1[1]);
                    Swap(ref data2[0], ref data2[1]);
                }

                return;
            }

            Sort(ref sortable[0], 0, sortable.Length - 1, ref data1[0], ref data2[0]);
        }

        private static void Sort<T1, T2>(ref float data0, int lo, int hi, ref T1 dataToSort1, ref T2 dataToSort2)
        {
            if (lo < hi)
            {
                int p = Partition(ref data0, lo, hi, ref dataToSort1, ref dataToSort2);
                Sort(ref data0, lo, p, ref dataToSort1, ref dataToSort2);
                Sort(ref data0, p + 1, hi, ref dataToSort1, ref dataToSort2);
            }
        }

        private static int Partition<T1, T2>(ref float data0, int lo, int hi, ref T1 dataToSort1, ref T2 dataToSort2)
        {
            float pivot = Unsafe.Add(ref data0, lo);
            int i = lo - 1;
            int j = hi + 1;
            while (true)
            {
                do
                {
                    i = i + 1;
                }
                while (Unsafe.Add(ref data0, i) < pivot && i < hi);

                do
                {
                    j = j - 1;
                }
                while (Unsafe.Add(ref data0, j) > pivot && j > lo);

                if (i >= j)
                {
                    return j;
                }

                Swap(ref Unsafe.Add(ref data0, i), ref Unsafe.Add(ref data0, j));
                Swap(ref Unsafe.Add(ref dataToSort1, i), ref Unsafe.Add(ref dataToSort1, j));
                Swap(ref Unsafe.Add(ref dataToSort2, i), ref Unsafe.Add(ref dataToSort2, j));
            }
        }
    }
}
