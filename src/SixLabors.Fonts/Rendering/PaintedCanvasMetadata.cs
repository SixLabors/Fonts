// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Canvas metadata describing the document-space coordinate system for a painted glyph.
/// </summary>
internal readonly struct PaintedCanvasMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaintedCanvasMetadata"/> struct.
    /// </summary>
    /// <param name="viewBox">The viewBox rectangle (minX, minY, width, height).</param>
    /// <param name="isYDown">True if the source coordinate system is y-down; false if y-up.</param>
    /// <param name="rootTransform">An optional root transform in document-space.</param>
    public PaintedCanvasMetadata(FontRectangle viewBox, bool isYDown, Matrix3x2 rootTransform)
    {
        this.HasViewBox = viewBox != FontRectangle.Empty;
        this.ViewBox = viewBox;
        this.IsYDown = isYDown;
        this.RootTransform = rootTransform;
    }

    /// <summary>
    /// Gets a value indicating whether a root viewBox is present.
    /// </summary>
    public bool HasViewBox { get; }

    /// <summary>
    /// Gets the viewBox.
    /// </summary>
    public FontRectangle ViewBox { get; }

    /// <summary>
    /// Gets a value indicating whether the source coordinate system is y-down.
    /// </summary>
    public bool IsYDown { get; }

    /// <summary>
    /// Gets the root transform in document-space.
    /// </summary>
    public Matrix3x2 RootTransform { get; }
}
