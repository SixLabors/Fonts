// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A glyph fully decomposed into painted layers ready for rendering.
/// </summary>
internal readonly struct PaintedGlyph
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaintedGlyph"/> struct.
    /// </summary>
    /// <param name="layers">The painted layers.</param>
    public PaintedGlyph(List<PaintedLayer> layers) => this.Layers = layers;

    /// <summary>
    /// Gets the layers for this glyph.
    /// </summary>
    public IReadOnlyList<PaintedLayer> Layers { get; }

    /// <summary>
    /// Gets a value indicating whether this glyph has no layers.
    /// </summary>
    public bool IsEmpty => this.Layers.Count == 0;

    /// <summary>
    /// Gets an empty glyph instance.
    /// </summary>
    public static PaintedGlyph Empty => new([]);
}
