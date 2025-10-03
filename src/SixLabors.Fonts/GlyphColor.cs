// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts;

/// <summary>
/// Provides access to the color details for the current glyph.
/// </summary>
public readonly struct GlyphColor
{
    internal GlyphColor(byte red, byte green, byte blue, byte alpha)
    {
        this.Red = red;
        this.Green = green;
        this.Blue = blue;
        this.Alpha = alpha;
    }

    /// <summary>
    /// Gets the red component
    /// </summary>
    public readonly byte Red { get; }

    /// <summary>
    /// Gets the green component
    /// </summary>
    public readonly byte Green { get; }

    /// <summary>
    /// Gets the blue component
    /// </summary>
    public readonly byte Blue { get; }

    /// <summary>
    /// Gets the alpha component
    /// </summary>
    public readonly byte Alpha { get; }

    /// <summary>
    /// Compares two <see cref="GlyphColor"/> objects for equality.
    /// </summary>
    /// <param name="left">
    /// The <see cref="GlyphColor"/> on the left side of the operand.
    /// </param>
    /// <param name="right">
    /// The <see cref="GlyphColor"/> on the right side of the operand.
    /// </param>
    /// <returns>
    /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    public static bool operator ==(GlyphColor left, GlyphColor right)
        => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="GlyphColor"/> objects for inequality.
    /// </summary>
    /// <param name="left">
    /// The <see cref="GlyphColor"/> on the left side of the operand.
    /// </param>
    /// <param name="right">
    /// The <see cref="GlyphColor"/> on the right side of the operand.
    /// </param>
    /// <returns>
    /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
    /// </returns>
    public static bool operator !=(GlyphColor left, GlyphColor right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is GlyphColor p && this.Equals(p);

    /// <summary>
    /// Compares the <see cref="GlyphColor"/> for equality to this color.
    /// </summary>
    /// <param name="other">
    /// The other <see cref="GlyphColor"/> to compare to.
    /// </param>
    /// <returns>
    /// True if the current color is equal to the <paramref name="other"/> parameter; otherwise, false.
    /// </returns>
    public bool Equals(GlyphColor other)
        => other.Red == this.Red
        && other.Green == this.Green
        && other.Blue == this.Blue
        && other.Alpha == this.Alpha;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(
            this.Red,
            this.Green,
            this.Blue,
            this.Alpha);

    /// <summary>
    /// Gets the hexadecimal string representation of the color instance in the format RRGGBBAA.
    /// </summary>
    /// <param name="value">
    /// The hexadecimal representation of the combined color components.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the <see cref="GlyphColor"/> equivalent of the hexadecimal input.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the parsing was successful; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParseHex(string? value, [NotNullWhen(true)] out GlyphColor? result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> hex = value.AsSpan();

        if (hex[0] != '#')
        {
            return false;
        }
        else
        {
            hex = hex[1..];
        }

        byte a = 255, r, g, b;

        switch (hex.Length)
        {
            case 8:
                if (!TryParseByte(hex[0], hex[1], out r) ||
                    !TryParseByte(hex[2], hex[3], out g) ||
                    !TryParseByte(hex[4], hex[5], out b) ||
                    !TryParseByte(hex[6], hex[7], out a))
                {
                    return false;
                }

                break;

            case 6:
                if (!TryParseByte(hex[0], hex[1], out r) ||
                    !TryParseByte(hex[2], hex[3], out g) ||
                    !TryParseByte(hex[4], hex[5], out b))
                {
                    return false;
                }

                break;

            case 4:
                if (!TryExpand(hex[0], out r) ||
                    !TryExpand(hex[1], out g) ||
                    !TryExpand(hex[2], out b) ||
                    !TryExpand(hex[3], out a))
                {
                    return false;
                }

                break;

            case 3:
                if (!TryExpand(hex[0], out r) ||
                    !TryExpand(hex[1], out g) ||
                    !TryExpand(hex[2], out b))
                {
                    return false;
                }

                break;

            default:
                return false;
        }

        result = new GlyphColor(r, g, b, a);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseByte(char hi, char lo, out byte value)
    {
        if (TryConvertHexCharToByte(hi, out byte high) && TryConvertHexCharToByte(lo, out byte low))
        {
            value = (byte)((high << 4) | low);
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryExpand(char c, out byte value)
    {
        if (TryConvertHexCharToByte(c, out byte nibble))
        {
            value = (byte)((nibble << 4) | nibble);
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertHexCharToByte(char c, out byte value)
    {
        if ((uint)(c - '0') <= 9)
        {
            value = (byte)(c - '0');
            return true;
        }

        char lower = (char)(c | 0x20); // Normalize to lowercase

        if ((uint)(lower - 'a') <= 5)
        {
            value = (byte)(lower - 'a' + 10);
            return true;
        }

        value = 0;
        return false;
    }
}
