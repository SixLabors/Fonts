// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Represents positioned glyph ids that share one set of glyph rendering options.
/// </summary>
public sealed class GlyphRun
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphRun"/> class.
    /// </summary>
    /// <param name="glyphIds">The glyph identifiers.</param>
    /// <param name="origins">The glyph origins.</param>
    public GlyphRun(ReadOnlyMemory<ushort> glyphIds, ReadOnlyMemory<Vector2> origins)
    {
        if (glyphIds.Length != origins.Length)
        {
            throw new ArgumentException("Glyph id and origin counts must match.", nameof(origins));
        }

        this.GlyphIds = glyphIds;
        this.Origins = origins;
    }

    /// <summary>
    /// Gets the glyph identifiers.
    /// </summary>
    public ReadOnlyMemory<ushort> GlyphIds { get; }

    /// <summary>
    /// Gets the glyph origins.
    /// </summary>
    public ReadOnlyMemory<Vector2> Origins { get; }

    /// <summary>
    /// Gets the number of glyphs in the run.
    /// </summary>
    public int Count => this.GlyphIds.Length;
}
