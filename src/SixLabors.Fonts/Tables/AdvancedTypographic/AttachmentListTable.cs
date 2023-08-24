// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal sealed class AttachmentListTable
    {
        public CoverageTable? CoverageTable { get; internal set; }

        public AttachPoint[]? AttachPoints { get; internal set; }

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

            var attachmentListTable = new AttachmentListTable
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
}
