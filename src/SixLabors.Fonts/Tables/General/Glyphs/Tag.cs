// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    /// <summary>
    /// Data type for tag identifiers. Tags are four byte integers, each byte representing a character.
    /// Tags are used to identify tables, design-variation axes, scripts, languages, font features, and baselines with
    /// human-readable names.
    /// </summary>
    internal readonly struct Tag : IEquatable<Tag>
    {
        public Tag(uint value) => this.Value = value;

        public uint Value { get; }

        public static implicit operator Tag(uint value) => new Tag(value);

        public static bool operator ==(Tag left, Tag right) => left.Equals(right);

        public static bool operator !=(Tag left, Tag right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Tag Parse(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 4)
            {
                return 0;
            }

            byte b3 = GetByte(value[3]);
            byte b2 = GetByte(value[2]);
            byte b1 = GetByte(value[1]);
            byte b0 = GetByte(value[0]);

            return (uint)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetByte(char c)
        {
            if (c > -1 && c < 256)
            {
                return (byte)c;
            }

            return 0;
        }

        public override bool Equals(object? obj) => obj is Tag tag && this.Equals(tag);

        public bool Equals(Tag other) => this.Value == other.Value;

        public override int GetHashCode() => HashCode.Combine(this.Value);

        public override string ToString()
        {
            char[] chars = new char[4];
            chars[3] = (char)(this.Value & 0xFF);
            chars[2] = (char)((this.Value >> 8) & 0xFF);
            chars[1] = (char)((this.Value >> 16) & 0xFF);
            chars[0] = (char)((this.Value >> 24) & 0xFF);

            return new string(chars);
        }
    }
}
