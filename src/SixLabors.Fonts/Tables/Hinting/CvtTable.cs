// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Hinting

{
    [TableName(TableName)]
    internal class CvtTable : Table
    {
        internal const string TableName = "cvt "; // space on the end of cvt is important/required

        public CvtTable(short[] controlValues)
        {
            this.ControlValues = controlValues;
        }

        public short[] ControlValues { get; }

        public static CvtTable? Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader, out TableHeader? header))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader, header.Length);
            }
        }

        public static CvtTable Load(BigEndianBinaryReader reader, uint tableLength)
        {
            // HEADER

            // Type     | Description
            // ---------| ------------
            // FWORD[n] | List of n values referenceable by instructions.n is the number of FWORD items that fit in the size of the table.

            var shortSize = sizeof(short);

            var itemCount = (int)(tableLength / shortSize);

            var controlValues = reader.ReadFWORDArray(itemCount);

            return new CvtTable(controlValues);
        }
    }
}
