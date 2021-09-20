// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal class AnchorTable
    {
        private AnchorTable()
        {
        }

        public static AnchorTable Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);
            ushort anchorFormat = reader.ReadUInt16();

            return anchorFormat switch
            {
                1 => Load(reader, offset),
                _ => throw new NotSupportedException($"anchorFormat {anchorFormat}. Should be '1'.")
            };
        }

        internal sealed class AnchorFormat1 : AnchorTable
        {
            public AnchorFormat1(short xCoordinate, short yCoordinate)
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

            public static AnchorFormat1 Load(BigEndianBinaryReader reader)
            {
                // +--------------+------------------------+------------------------------------------------+
                // | Type         | Name                   | Description                                    |
                // +==============+========================+================================================+
                // | uint16       | anchorFormat           | Format identifier, = 1                         |
                // +--------------+------------------------+------------------------------------------------+
                // | int16        | xCoordinate            | Horizontal value, in design units.             |
                // +--------------+------------------------+------------------------------------------------+
                // |int16         | yCoordinate            | Vertical value, in design units.               |
                // +--------------+------------------------+------------------------------------------------+
                short xCoordinate = reader.ReadInt16();
                short yCoordinate = reader.ReadInt16();
                return new AnchorFormat1(xCoordinate, yCoordinate);
            }
        }
    }
}
