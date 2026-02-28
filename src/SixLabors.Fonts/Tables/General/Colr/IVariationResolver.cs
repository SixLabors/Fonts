// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#pragma warning disable SA1201 // Elements should appear in the correct order
namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Provides a mechanism to resolve variation index deltas.
/// </summary>
internal interface IVariationResolver
{
    /// <summary>
    /// Calculates the resolved delta value for the specified variable index.
    /// </summary>
    /// <param name="varIndex">The zero-based index of the variable for which to resolve the delta value.</param>
    /// <returns>The resolved delta value as a floating-point number for the specified variable index.</returns>
    public float ResolveDelta(uint varIndex);
}
