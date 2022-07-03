// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.TrueType.Hinting
{
    internal class CvtTable : Table
    {
        internal const string TableName = "cvt "; // space on the end of cvt is important/required

        public CvtTable(short[] controlValues) => this.ControlValues = controlValues;

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
            const int shortSize = sizeof(short);

            int itemCount = (int)(tableLength / shortSize);

            short[] controlValues = reader.ReadFWORDArray(itemCount);

            return new CvtTable(controlValues);
        }
    }
}
