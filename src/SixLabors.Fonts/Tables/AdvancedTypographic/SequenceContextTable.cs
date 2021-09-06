// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal class SequenceContextTable
    {
        private SequenceContextTable()
        {
        }

        public static CoverageTable Load(BigEndianBinaryReader reader, long offset)
        {
            throw new NotImplementedException();
        }
    }

    internal class SequenceContextFormat1Table
    {
        public static SequenceContextFormat1Table Load(BigEndianBinaryReader reader, long offset)
        {
            // SequenceContextFormat1
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | Type     | Name                               | Description                                                   |
            // +==========+====================================+===============================================================+
            // | uint16   | format                             | Format identifier: format = 1                                 |
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | Offset16 | coverageOffset                     | Offset to Coverage table, from beginning of                   |
            // |          |                                    | SequenceContextFormat1 table                                  |
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | uint16   | seqRuleSetCount                    | Number of SequenceRuleSet tables                              |
            // +----------+------------------------------------+---------------------------------------------------------------+
            // | Offset16 | seqRuleSetOffsets[seqRuleSetCount] | Array of offsets to SequenceRuleSet tables, from beginning of |
            // |          |                                    | SequenceContextFormat1 table (offsets may be NULL)            |
            // +----------+------------------------------------+---------------------------------------------------------------+
            ushort coverageOffset = reader.ReadOffset16();
            ushort seqRuleSetCount = reader.ReadUInt16();
            ushort[] seqRuleSetOffsets = reader.ReadUInt16Array(seqRuleSetCount);

            // SequenceRuleSet
            // +----------+------------------------------+----------------------------------------------------------------+
            // | Type     | Name                         | Description                                                    |
            // +==========+==============================+================================================================+
            // | uint16   | seqRuleCount                 | Number of SequenceRule tables                                  |
            // +----------+------------------------------+----------------------------------------------------------------+
            // | Offset16 | seqRuleOffsets[posRuleCount] | Array of offsets to SequenceRule tables, from beginning of the |
            // |          |                              | SequenceRuleSet table                                          |
            // +----------+------------------------------+----------------------------------------------------------------+
            throw new NotImplementedException();
        }
    }
}
