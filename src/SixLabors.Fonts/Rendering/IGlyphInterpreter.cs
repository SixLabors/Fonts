// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Interface for format-specific glyph interpreters that build painted layers.
/// Implementations live in Fonts (e.g., OT-SVG, COLR v1) and must:
/// <list type="bullet">
/// <item><description>Resolve all palette references to RGBA colors.</description></item>
/// <item><description>Pre-apply all transforms (element/group, gradient transforms, layout scale/rotation/translation).</description></item>
/// <item><description>Normalize gradient coordinates and flags (see <see cref="GradientUnits"/>).</description></item>
/// <item><description>Adjust arc parameters for the final transform (uniform scale/rotation/reflection).</description></item>
/// </list>
/// The resulting <see cref="PaintedGlyph"/> must be ready to stream to a renderer with no further format-specific logic.
/// </summary>
internal interface IGlyphInterpreter
{
    /// <summary>
    /// Attempts to build a <see cref="PaintedGlyph"/> for the specified glyph identifier.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="glyph">When this method returns, contains the painted glyph if available; otherwise, <see cref="PaintedGlyph.Empty"/>.</param>
    /// <returns><see langword="true"/> if painted layers were built; otherwise, <see langword="false"/>.</returns>
    public bool TryBuild(ushort glyphId, [NotNullWhen(true)] out PaintedGlyph? glyph);
}
