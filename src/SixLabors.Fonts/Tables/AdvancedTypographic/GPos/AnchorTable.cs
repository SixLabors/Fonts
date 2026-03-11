// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Numerics;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

[DebuggerDisplay("X: {XCoordinate}, Y: {YCoordinate}")]
internal abstract class AnchorTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnchorTable"/> class.
    /// </summary>
    /// <param name="xCoordinate">The horizontal value, in design units.</param>
    /// <param name="yCoordinate">The vertical value, in design units.</param>
    protected AnchorTable(short xCoordinate, short yCoordinate)
    {
        this.XCoordinate = xCoordinate;
        this.YCoordinate = yCoordinate;
    }

    /// <summary>
    /// Gets the horizontal value, in design units.
    /// </summary>
    protected short XCoordinate { get; }

    /// <summary>
    /// Gets the vertical value, in design units.
    /// </summary>
    protected short YCoordinate { get; }

    public abstract AnchorXY GetAnchor(FontMetrics fontMetrics, GlyphShapingData data, GlyphPositioningCollection collection);

    /// <summary>
    /// Loads the anchor table.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the anchor table.</param>
    /// <returns>The anchor table.</returns>
    public static AnchorTable Load(BigEndianBinaryReader reader, long offset)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort anchorFormat = reader.ReadUInt16();

        return anchorFormat switch
        {
            1 => AnchorFormat1.Load(reader),
            2 => AnchorFormat2.Load(reader),
            3 => AnchorFormat3.LoadFormat3(reader, offset),

            // Harfbuzz (Anchor.hh) treats this as an empty table and does not throw..
            // NotoSans Regular can trigger this. See https://github.com/SixLabors/Fonts/issues/417
            _ => EmptyAnchorTable.Instance,
        };
    }

    internal sealed class AnchorFormat1 : AnchorTable
    {
        public AnchorFormat1(short xCoordinate, short yCoordinate)
            : base(xCoordinate, yCoordinate)
        {
        }

        public static AnchorFormat1 Load(BigEndianBinaryReader reader)
        {
            // +--------------+------------------------+------------------------------------------------+
            // | Type         | Name                   | Description                                    |
            // +==============+========================+================================================+
            // | uint16       | anchorFormat           | Format identifier, = 1                         |
            // +--------------+------------------------+------------------------------------------------+
            // | int16        | xCoordinate            | Horizontal value, in design units.             |
            // +--------------+------------------------+------------------------------------------------+
            // | int16        | yCoordinate            | Vertical value, in design units.               |
            // +--------------+------------------------+------------------------------------------------+
            short xCoordinate = reader.ReadInt16();
            short yCoordinate = reader.ReadInt16();
            return new AnchorFormat1(xCoordinate, yCoordinate);
        }

        public override AnchorXY GetAnchor(FontMetrics fontMetrics, GlyphShapingData data, GlyphPositioningCollection collection)
            => new(this.XCoordinate, this.YCoordinate);
    }

    internal sealed class AnchorFormat2 : AnchorTable
    {
        private readonly ushort anchorPointIndex;

        public AnchorFormat2(short xCoordinate, short yCoordinate, ushort anchorPointIndex)
            : base(xCoordinate, yCoordinate) => this.anchorPointIndex = anchorPointIndex;

        public static AnchorFormat2 Load(BigEndianBinaryReader reader)
        {
            // +--------------+------------------------+------------------------------------------------+
            // | Type         | Name                   | Description                                    |
            // +==============+========================+================================================+
            // | uint16       | anchorFormat           | Format identifier, = 2                         |
            // +--------------+------------------------+------------------------------------------------+
            // | int16        | xCoordinate            | Horizontal value, in design units.             |
            // +--------------+------------------------+------------------------------------------------+
            // | int16        | yCoordinate            | Vertical value, in design units.               |
            // +--------------+------------------------+------------------------------------------------+
            // | uint16       + anchorPoint            | Index to glyph contour point.                  +
            // +--------------+------------------------+------------------------------------------------+
            short xCoordinate = reader.ReadInt16();
            short yCoordinate = reader.ReadInt16();
            ushort anchorPointIndex = reader.ReadUInt16();
            return new AnchorFormat2(xCoordinate, yCoordinate, anchorPointIndex);
        }

        public override AnchorXY GetAnchor(FontMetrics fontMetrics, GlyphShapingData data, GlyphPositioningCollection collection)
        {
            if (collection.TextOptions.HintingMode != HintingMode.None)
            {
                TextAttributes textAttributes = data.TextRun.TextAttributes;
                TextDecorations textDecorations = data.TextRun.TextDecorations;
                LayoutMode layoutMode = collection.TextOptions.LayoutMode;
                ColorFontSupport colorFontSupport = collection.TextOptions.ColorFontSupport;
                if (fontMetrics.TryGetGlyphMetrics(data.CodePoint, textAttributes, textDecorations, layoutMode, colorFontSupport, out GlyphMetrics? metrics))
                {
                    if (metrics is TrueTypeGlyphMetrics ttmetric)
                    {
                        IList<ControlPoint> points = ttmetric.GetOutline().ControlPoints;
                        if (this.anchorPointIndex < points.Count)
                        {
                            Vector2 point = points[this.anchorPointIndex].Point;
                            return new((short)point.X, (short)point.Y);
                        }
                    }
                }
            }

            return new(this.XCoordinate, this.YCoordinate);
        }
    }

    internal sealed class AnchorFormat3 : AnchorTable
    {
        private const ushort VariationIndexFormat = 0x8000;

        /// <summary>
        /// Packed VariationIndex for X: (outerIndex &lt;&lt; 16) | innerIndex. 0 = none.
        /// </summary>
        private readonly uint xVariation;

        /// <summary>
        /// Packed VariationIndex for Y: (outerIndex &lt;&lt; 16) | innerIndex. 0 = none.
        /// </summary>
        private readonly uint yVariation;

        public AnchorFormat3(short xCoordinate, short yCoordinate, uint xVariation, uint yVariation)
            : base(xCoordinate, yCoordinate)
        {
            this.xVariation = xVariation;
            this.yVariation = yVariation;
        }

        public static AnchorFormat3 LoadFormat3(BigEndianBinaryReader reader, long anchorBase)
        {
            // +--------------+------------------------+-----------------------------------------------------------+
            // | Type         | Name                   | Description                                               |
            // +==============+========================+===========================================================+
            // | uint16       | anchorFormat           | Format identifier, = 3                                    |
            // +--------------+------------------------+-----------------------------------------------------------+
            // | int16        | xCoordinate            | Horizontal value, in design units.                        |
            // +--------------+------------------------+-----------------------------------------------------------+
            // | int16        | yCoordinate            | Vertical value, in design units.                          |
            // +--------------+------------------------+-----------------------------------------------------------+
            // | Offset16     | xDeviceOffset          + Offset to Device table (non-variable font) /              |
            // |              |                        | VariationIndex table (variable font) for X coordinate,    |
            // |              |                        | from beginning of Anchor table (may be NULL)              |
            // +--------------+------------------------+-----------------------------------------------------------+
            // | Offset16     | yDeviceOffset          + Offset to Device table (non-variable font) /              |
            // |              |                        | VariationIndex table (variable font) for Y coordinate,    |
            // |              |                        | from beginning of Anchor table (may be NULL)              |
            // +--------------+------------------------+-----------------------------------------------------------+
            short xCoordinate = reader.ReadInt16();
            short yCoordinate = reader.ReadInt16();
            ushort xDeviceOffset = reader.ReadOffset16();
            ushort yDeviceOffset = reader.ReadOffset16();

            uint xVariation = ResolveVariationIndex(reader, anchorBase, xDeviceOffset);
            uint yVariation = ResolveVariationIndex(reader, anchorBase, yDeviceOffset);

            return new AnchorFormat3(xCoordinate, yCoordinate, xVariation, yVariation);
        }

        public override AnchorXY GetAnchor(FontMetrics fontMetrics, GlyphShapingData data, GlyphPositioningCollection collection)
        {
            short x = this.XCoordinate;
            short y = this.YCoordinate;

            if (this.xVariation != 0)
            {
                x += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(this.xVariation));
            }

            if (this.yVariation != 0)
            {
                y += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(this.yVariation));
            }

            return new(x, y);
        }

        private static uint ResolveVariationIndex(BigEndianBinaryReader reader, long anchorBase, ushort deviceOffset)
        {
            if (deviceOffset == 0)
            {
                return 0;
            }

            long savedPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = anchorBase + deviceOffset;

            ushort first = reader.ReadUInt16();
            ushort second = reader.ReadUInt16();
            ushort format = reader.ReadUInt16();

            reader.BaseStream.Position = savedPosition;

            if (format == VariationIndexFormat)
            {
                return ((uint)first << 16) | second;
            }

            // TODO: Device table (per-ppem adjustments) — not yet implemented.
            return 0;
        }
    }

    internal sealed class EmptyAnchorTable : AnchorTable
    {
        private EmptyAnchorTable()
            : base(0, 0)
        {
        }

        public static EmptyAnchorTable Instance { get; } = new();

        public override AnchorXY GetAnchor(
            FontMetrics fontMetrics,
            GlyphShapingData data,
            GlyphPositioningCollection collection)
            => new(0, 0);
    }
}
