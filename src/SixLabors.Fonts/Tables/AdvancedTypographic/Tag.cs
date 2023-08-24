// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Data type for tag identifiers. Tags are four byte integers, each byte representing a character.
/// Tags are used to identify tables, design-variation axes, scripts, languages, font features, and baselines with
/// human-readable names.
/// </summary>
public readonly struct Tag : IEquatable<Tag>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> struct.
    /// </summary>
    /// <param name="value">The tag value.</param>
    public Tag(uint value) => this.Value = value;

    /// <summary>
    /// Gets the Tag value as 32 bit unsigned integer.
    /// </summary>
    public uint Value { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static implicit operator Tag(uint value) => new(value);

    public static implicit operator Tag(FeatureTags value) => new((uint)value);

    public static bool operator ==(Tag left, Tag right) => left.Equals(right);

    public static bool operator !=(Tag left, Tag right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Converts the string representation of a number to its Tag equivalent.
    /// </summary>
    /// <param name="value">A string containing a tag to convert.</param>
    /// <returns>The <see cref="Tag"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tag Parse(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 4)
        {
            return default;
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
        if (c is >= (char)0 and <= (char)255)
        {
            return (byte)c;
        }

        return 0;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Tag tag && this.Equals(tag);

    /// <inheritdoc/>
    public bool Equals(Tag other) => this.Value == other.Value;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.Value);

    /// <inheritdoc/>
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
