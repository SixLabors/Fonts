// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
    internal ref struct SimpleBinaryReader
    {
        private readonly ReadOnlySpan<byte> buffer;

        public SimpleBinaryReader(ReadOnlySpan<byte> buffer)
        {
            this.buffer = buffer;
            this.Position = 0;
        }

        public int Length => this.buffer.Length;

        // TODO: Bounds checks.
        public int Position { get; set; }

        public bool CanRead() => (uint)this.Position < this.buffer.Length;

        public byte ReadByte() => this.buffer[this.Position++];

        public int ReadInt16BE()
        {
            byte b1 = this.buffer[this.Position + 1];
            byte b0 = this.buffer[this.Position];
            this.Position += 2;

            return (short)((b0 << 8) | b1);
        }

        public float ReadFloatFixed1616()
        {
            // Read a BE int, we parse it later.
            byte b3 = this.buffer[this.Position + 3];
            byte b2 = this.buffer[this.Position + 2];
            byte b1 = this.buffer[this.Position + 1];
            byte b0 = this.buffer[this.Position];
            this.Position += 4;

            // This number is interpreted as a Fixed; that is, a signed number with 16 bits of fraction
            float number = (short)((b0 << 8) | b1);
            float fraction = (short)((b2 << 8) | b3) / 65536F;
            return number + fraction;
        }
    }
}
