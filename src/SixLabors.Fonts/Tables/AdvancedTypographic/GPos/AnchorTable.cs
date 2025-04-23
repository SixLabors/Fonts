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
    private static readonly AnchorTable Empty = new EmptyAnchor();

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
            3 => AnchorFormat3.Load(reader),

            // Harfbuzz (Anchor.hh) and FontKit appear to treat this as a default anchor and do not throw.
            // NotoSans Regular can trigger this. See https://github.com/SixLabors/Fonts/issues/417
            _ => Empty,
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
                if (fontMetrics.TryGetGlyphMetrics(data.CodePoint, textAttributes, textDecorations, layoutMode, colorFontSupport, out IReadOnlyList<GlyphMetrics>? metrics))
                {
                    foreach (GlyphMetrics metric in metrics)
                    {
                        if (metric is not TrueTypeGlyphMetrics ttmetric)
                        {
                            break;
                        }

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
        // TODO: actually use the xDeviceOffset.
        private readonly ushort xDeviceOffset;

        // TODO: actually use the yDeviceOffset.
        private readonly ushort yDeviceOffset;

        public AnchorFormat3(short xCoordinate, short yCoordinate, ushort xDeviceOffset, ushort yDeviceOffset)
            : base(xCoordinate, yCoordinate)
        {
            this.xDeviceOffset = xDeviceOffset;
            this.yDeviceOffset = yDeviceOffset;
        }

        public static AnchorFormat3 Load(BigEndianBinaryReader reader)
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
            // | uint16       + anchorPoint            | Index to glyph contour point.                             +
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
            return new AnchorFormat3(xCoordinate, yCoordinate, xDeviceOffset, yDeviceOffset);
        }

        public override AnchorXY GetAnchor(FontMetrics fontMetrics, GlyphShapingData data, GlyphPositioningCollection collection)
            => new(this.XCoordinate, this.YCoordinate);
    }

    internal sealed class EmptyAnchor : AnchorTable
    {
        public EmptyAnchor()
            : base(0, 0)
        {
        }

        public override AnchorXY GetAnchor(
            FontMetrics fontMetrics,
            GlyphShapingData data,
            GlyphPositioningCollection collection)
            => new(0, 0);
    }
}
