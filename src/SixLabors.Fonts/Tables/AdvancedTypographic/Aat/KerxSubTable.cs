// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// Applies one extended kerning subtable.
/// </summary>
internal abstract partial class KerxSubTable
{
    private readonly CoverageFlags coverage;

    /// <summary>
    /// Initializes a new instance of the <see cref="KerxSubTable"/> class.
    /// </summary>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    private KerxSubTable(byte coverage) => this.coverage = (CoverageFlags)coverage;

    /// <summary>
    /// Flags in the high byte of the kerx coverage field.
    /// </summary>
    [Flags]
    private enum CoverageFlags : byte
    {
        /// <summary>
        /// Processes glyphs in reverse visual order.
        /// </summary>
        Backwards = 0x10,

        /// <summary>
        /// Applies an offset perpendicular to the advance direction.
        /// </summary>
        CrossStream = 0x40,

        /// <summary>
        /// Applies the subtable to vertical runs.
        /// </summary>
        Vertical = 0x80
    }

    /// <summary>
    /// Gets a value indicating whether the subtable applies in vertical text.
    /// </summary>
    public bool IsVertical => (this.coverage & CoverageFlags.Vertical) != 0;

    /// <summary>
    /// Gets a value indicating whether adjustments run perpendicular to the advance.
    /// </summary>
    public bool IsCrossStream => (this.coverage & CoverageFlags.CrossStream) != 0;

    /// <summary>
    /// Gets a value indicating whether the machine visits the visual stream backwards.
    /// </summary>
    public bool IsBackwards => (this.coverage & CoverageFlags.Backwards) != 0;

    /// <summary>
    /// Loads the format-specific data within the subtable's directory bounds.
    /// </summary>
    /// <param name="reader">The table reader.</param>
    /// <param name="start">The offset of the first byte in the subtable.</param>
    /// <param name="end">The offset of the first byte after the table.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <param name="format">The kerx subtable format.</param>
    /// <param name="coverage">The coverage flags for the subtable.</param>
    /// <param name="tupleCount">The number of values per kerning tuple.</param>
    /// <returns>The subtable, or <see langword="null"/> if the format or data is invalid.</returns>
    public static KerxSubTable? Load(BigEndianBinaryReader reader, long start, long end, int glyphCount, byte format, byte coverage, uint tupleCount)
        => format switch
        {
            0 => LoadPairs(reader, start, end, coverage, tupleCount),
            2 or 6 => LoadMatrix(reader, start, end, glyphCount, format, coverage, tupleCount),
            1 or 4 => LoadState(reader, start, end, glyphCount, format, coverage, tupleCount),
            _ => null
        };

    /// <summary>
    /// Applies the subtable to the glyphs in traversal order.
    /// </summary>
    /// <param name="font">The font metrics for the run.</param>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="traversal">The glyph indices in subtable order.</param>
    /// <param name="requestedKerning">Whether the text options enable kerning.</param>
    /// <param name="anchors">The anchor table, if present.</param>
    /// <param name="crossStream">The cross-stream adjustments indexed from the start of the run.</param>
    /// <returns>Whether the subtable changed a live glyph's position or attachment.</returns>
    public abstract bool Apply(FontMetrics font, ShapingBuffer buffer, AatStateTable.Traversal traversal, bool requestedKerning, AatAnchorTable? anchors, scoped Span<(int Value, bool Reset)> crossStream);

    /// <summary>
    /// Records a cross-stream adjustment or a reset of the running offset.
    /// </summary>
    /// <param name="adjustment">The pending adjustment for the record.</param>
    /// <param name="value">The adjustment in design units or the reset value.</param>
    /// <param name="accumulate">Whether to add the value to the pending adjustment.</param>
    private static void AddCrossStream(ref (int Value, bool Reset) adjustment, int value, bool accumulate)
    {
        // The legacy kern example uses the signed word 0x8000 to reset the
        // carried cross-stream offset. Keep the glyph attachment links.
        if (value == short.MinValue)
        {
            adjustment = (0, true);
        }
        else
        {
            adjustment.Value = accumulate ? adjustment.Value + value : value;
        }
    }
}
