// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represent the metrics of a font face specific to vertical text.
/// </summary>
public class VerticalMetrics : IMetricsHeader
{
    /// <inheritdoc/>
    public short Ascender { get; internal set; }

    /// <inheritdoc/>
    public short Descender { get; internal set; }

    /// <inheritdoc/>
    public short LineGap { get; internal set; }

    /// <inheritdoc/>
    public short LineHeight { get; internal set; }

    /// <inheritdoc/>
    public short AdvanceWidthMax { get; internal set; }

    /// <inheritdoc/>
    public short AdvanceHeightMax { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the metrics have been synthesized.
    /// </summary>
    internal bool Synthesized { get; set; }
}
