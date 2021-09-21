// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    [DebuggerDisplay("X: {XCoordinate}, Y: {YCoordinate}")]
    internal class AnchorTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnchorTable"/> class.
        /// </summary>
        /// <param name="xCoordinate">The horizontal value, in design units.</param>
        /// <param name="yCoordinate">The vertical value, in design units.</param>
        public AnchorTable(short xCoordinate, short yCoordinate)
        {
            this.XCoordinate = xCoordinate;
            this.YCoordinate = yCoordinate;
        }

        /// <summary>
        /// Gets the horizontal value, in design units.
        /// </summary>
        public short XCoordinate { get; }

        /// <summary>
        /// Gets the vertical value, in design units.
        /// </summary>
        public short YCoordinate { get; }

        /// <summary>
        /// Loads the anchor table.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the beginning of the anchor table.</param>
        /// <returns>The anchor table.</returns>
        public static AnchorTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            ushort anchorFormat = reader.ReadUInt16();

            return anchorFormat switch
            {
                1 => AnchorFormat1.Load(reader),
                _ => throw new NotSupportedException($"anchorFormat {anchorFormat} not supported. Should be '1'.")
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
        }
    }
}
