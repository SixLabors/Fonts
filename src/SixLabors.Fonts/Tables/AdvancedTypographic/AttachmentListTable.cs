// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The Attachment List table (AttachList) identifies all the attachment points defined in the GDEF table
/// and their associated glyphs so a client can quickly access coordinates for each glyph's attachment points.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table"/>
/// </summary>
internal sealed class AttachmentListTable
{
    /// <summary>
    /// Gets or sets the coverage table that defines which glyphs have attachment points.
    /// </summary>
    public CoverageTable? CoverageTable { get; internal set; }

    /// <summary>
    /// Gets or sets the array of attachment point tables, one per covered glyph, in Coverage Index order.
    /// </summary>
    public AttachPoint[]? AttachPoints { get; internal set; }

    /// <summary>
    /// Loads the <see cref="AttachmentListTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the GDEF table to the AttachList table.</param>
    /// <returns>The <see cref="AttachmentListTable"/>.</returns>
    public static AttachmentListTable Load(BigEndianBinaryReader reader, long offset)
    {
        // Attachment Point List Table
        // Type      | Name                           | Description
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | coverageOffset                 | Offset to Coverage table -from beginning of AttachList table.
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | glyphCount                     | Number of glyphs with attachment points.
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | attachPointOffsets[glyphCount] | Array of offsets to AttachPoint tables-from beginning of AttachList table-in Coverage Index order.
        // ----------|--------------------------------|--------------------------------------------------------------------------------------------------------
        reader.Seek(offset, SeekOrigin.Begin);

        ushort coverageOffset = reader.ReadUInt16();
        ushort glyphCount = reader.ReadUInt16();

        using Buffer<ushort> attachPointOffsetsBuffer = new(glyphCount);
        Span<ushort> attachPointOffsets = attachPointOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(attachPointOffsets);

        AttachmentListTable attachmentListTable = new()
        {
            CoverageTable = CoverageTable.Load(reader, offset + coverageOffset),
            AttachPoints = new AttachPoint[glyphCount]
        };

        for (int i = 0; i < glyphCount; ++i)
        {
            reader.Seek(offset + attachPointOffsets[i], SeekOrigin.Begin);
            ushort pointCount = reader.ReadUInt16();
            attachmentListTable.AttachPoints[i] = new AttachPoint()
            {
                PointIndices = reader.ReadUInt16Array(pointCount)
            };
        }

        return attachmentListTable;
    }
}
