// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Renderer contract for layered paint-aware glyphs. Implemented by Drawing renderers.
/// Geometry methods are inherited from <see cref="IGlyphRenderer"/>.
/// Rendering code should call <see cref="BeginLayer(Paint, FillRule)"/>,
/// stream geometry commands for that layer, then call <see cref="EndLayer"/>.
/// </summary>
public interface ILayeredGlyphRenderer : IGlyphRenderer
{
    /// <summary>
    /// Begins a new painted layer with the specified paint and fill rule.
    /// All geometry commands issued after this call belong to the layer until <see cref="EndLayer"/> is called.
    /// </summary>
    /// <param name="paint">The paint definition.</param>
    /// <param name="fillRule">The fill rule.</param>
    public void BeginLayer(Paint? paint, FillRule fillRule);

    /// <summary>
    /// Ends the current painted layer and rasterizes it.
    /// </summary>
    public void EndLayer();
}
