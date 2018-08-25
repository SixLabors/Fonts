// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.General.Kern
{
    internal readonly struct KearningPair : IComparable<KearningPair>
    {
        internal KearningPair(ushort left, ushort right, short offset)
        {
            this.Left = left;
            this.Right = right;
            this.Offset = offset;
            this.Key = CalculateKey(left, right);
        }

        public uint Key { get; }

        public ushort Left { get; }

        public ushort Right { get; }

        public short Offset { get; }

        public static uint CalculateKey(ushort left, ushort right)
        {
            uint value = (uint)(left << 16);
            return value + right;
        }

        public static KearningPair Read(BinaryReader reader)
        {
            // Type   | Field | Description
            // -------|-------|-------------------------------
            // uint16 | left  | The glyph index for the left-hand glyph in the kerning pair.
            // uint16 | right | The glyph index for the right-hand glyph in the kerning pair.
            // FWORD  | value | The kerning value for the above pair, in FUnits.If this value is greater than zero, the characters will be moved apart.If this value is less than zero, the character will be moved closer together.
            return new KearningPair(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadFWORD());
        }

        public int CompareTo(KearningPair other)
        {
            return this.Key.CompareTo(other.Key);
        }
    }
}
