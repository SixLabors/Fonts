// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Numerics;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Base class for GPOS Anchor tables that define an attachment point using X and Y coordinates.
/// Anchor tables are used by mark attachment and cursive attachment positioning subtables.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-tables"/>
/// </summary>
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

    /// <summary>
    /// Gets the resolved anchor coordinates, potentially adjusted for hinting or variation data.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="data">The glyph shaping data.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <returns>The resolved anchor coordinates.</returns>
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

    /// <summary>
    /// Anchor Table Format 1: design units only. Simple X, Y coordinate anchor point.
    /// </summary>
    internal sealed class AnchorFormat1 : AnchorTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnchorFormat1"/> class.
        /// </summary>
        /// <param name="xCoordinate">The horizontal value, in design units.</param>
        /// <param name="yCoordinate">The vertical value, in design units.</param>
        public AnchorFormat1(short xCoordinate, short yCoordinate)
            : base(xCoordinate, yCoordinate)
        {
        }

        /// <summary>
        /// Loads the Format 1 anchor table from the reader.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <returns>The loaded <see cref="AnchorFormat1"/>.</returns>
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

        /// <inheritdoc/>
        public override AnchorXY GetAnchor(FontMetrics fontMetrics, GlyphShapingData data, GlyphPositioningCollection collection)
            => new(this.XCoordinate, this.YCoordinate);
    }

    /// <summary>
    /// Anchor Table Format 2: design units plus contour point.
    /// Uses a glyph contour point index to determine the anchor position when hinting is enabled.
    /// </summary>
    internal sealed class AnchorFormat2 : AnchorTable
    {
        private readonly ushort anchorPointIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnchorFormat2"/> class.
        /// </summary>
        /// <param name="xCoordinate">The horizontal value, in design units.</param>
        /// <param name="yCoordinate">The vertical value, in design units.</param>
        /// <param name="anchorPointIndex">The index to the glyph contour point.</param>
        public AnchorFormat2(short xCoordinate, short yCoordinate, ushort anchorPointIndex)
            : base(xCoordinate, yCoordinate) => this.anchorPointIndex = anchorPointIndex;

        /// <summary>
        /// Loads the Format 2 anchor table from the reader.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <returns>The loaded <see cref="AnchorFormat2"/>.</returns>
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

        /// <inheritdoc/>
        public override AnchorXY GetAnchor(FontMetrics fontMetrics, GlyphShapingData data, GlyphPositioningCollection collection)
        {
            if (collection.TextOptions.HintingMode != HintingMode.None)
            {
                TextAttributes textAttributes = data.TextRun.TextAttributes;
                TextDecorations textDecorations = data.TextRun.TextDecorations;
                LayoutMode layoutMode = collection.TextOptions.LayoutMode;
                ColorFontSupport colorFontSupport = collection.TextOptions.ColorFontSupport;
                if (fontMetrics.TryGetGlyphMetrics(data.CodePoint, textAttributes, textDecorations, layoutMode, colorFontSupport, out FontGlyphMetrics? metrics))
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

    /// <summary>
    /// Anchor Table Format 3: design units plus Device/VariationIndex tables.
    /// Supports per-ppem adjustments via Device tables or variable font adjustments via VariationIndex tables.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AnchorFormat3"/> class.
        /// </summary>
        /// <param name="xCoordinate">The horizontal value, in design units.</param>
        /// <param name="yCoordinate">The vertical value, in design units.</param>
        /// <param name="xVariation">The packed VariationIndex for X coordinate.</param>
        /// <param name="yVariation">The packed VariationIndex for Y coordinate.</param>
        public AnchorFormat3(short xCoordinate, short yCoordinate, uint xVariation, uint yVariation)
            : base(xCoordinate, yCoordinate)
        {
            this.xVariation = xVariation;
            this.yVariation = yVariation;
        }

        /// <summary>
        /// Loads the Format 3 anchor table from the reader.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="anchorBase">The absolute stream position of the anchor table start.</param>
        /// <returns>The loaded <see cref="AnchorFormat3"/>.</returns>
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

        /// <inheritdoc/>
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

        /// <summary>
        /// Reads a Device/VariationIndex table at the given offset and returns a packed VariationIndex
        /// if it is a VariationIndex table (deltaFormat == 0x8000), or 0 otherwise.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="anchorBase">The absolute stream position of the anchor table.</param>
        /// <param name="deviceOffset">The offset to the Device/VariationIndex table from the anchor table base.</param>
        /// <returns>The packed VariationIndex, or 0 if not applicable.</returns>
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

    /// <summary>
    /// An empty anchor table that always returns (0, 0). Used as a fallback for unrecognized anchor formats.
    /// </summary>
    internal sealed class EmptyAnchorTable : AnchorTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyAnchorTable"/> class.
        /// </summary>
        private EmptyAnchorTable()
            : base(0, 0)
        {
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="EmptyAnchorTable"/>.
        /// </summary>
        public static EmptyAnchorTable Instance { get; } = new();

        /// <inheritdoc/>
        public override AnchorXY GetAnchor(
            FontMetrics fontMetrics,
            GlyphShapingData data,
            GlyphPositioningCollection collection)
            => new(0, 0);
    }
}
