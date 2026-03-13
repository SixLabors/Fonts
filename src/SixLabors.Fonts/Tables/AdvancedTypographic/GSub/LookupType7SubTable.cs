// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// This lookup provides a mechanism whereby any other lookup type’s subtables are stored at a 32-bit offset location
/// in the GSUB table. This is needed if the total size of the subtables exceeds the 16-bit limits of the various
/// other offsets in the GSUB table. In this specification, the subtable stored at the 32-bit offset location is
/// termed the "extension" subtable.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-7-extension-substitution"/>
/// </summary>
internal static class LookupType7SubTable
{
    /// <summary>
    /// Loads the extension substitution lookup subtable from the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <param name="subTableLoader">The delegate used to load the referenced extension subtable.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(
        BigEndianBinaryReader reader,
        long offset,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        Func<ushort, LookupFlags, ushort, BigEndianBinaryReader, long, LookupSubTable> subTableLoader)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort substFormat = reader.ReadUInt16();

        return substFormat switch
        {
            1 => LookupType7Format1SubTable.Load(reader, offset, lookupFlags, markFilteringSet, subTableLoader),
            _ => new NotImplementedSubTable(),
        };
    }
}

/// <summary>
/// Implements extension substitution format 1. This format provides a 32-bit offset to an
/// extension subtable of any other lookup type, enabling subtables that exceed the 16-bit
/// offset limit.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#71-extension-substitution-subtable-format-1"/>
/// </summary>
internal static class LookupType7Format1SubTable
{
    /// <summary>
    /// Loads the extension substitution format 1 subtable and resolves the referenced extension subtable.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the extension substitution subtable.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <param name="subTableLoader">The delegate used to load the referenced extension subtable.</param>
    /// <returns>The loaded <see cref="LookupSubTable"/>.</returns>
    public static LookupSubTable Load(
        BigEndianBinaryReader reader,
        long offset,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        Func<ushort, LookupFlags, ushort, BigEndianBinaryReader, long, LookupSubTable> subTableLoader)
    {
        // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
        // | Type     | Name                | Description                                                                                                                        |
        // +==========+=====================+====================================================================================================================================+
        // | uint16   | substFormat         | Format identifier. Set to 1.                                                                                                       |
        // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
        // | uint16   | extensionLookupType | Lookup type of subtable referenced by extensionOffset (that is, the extension subtable).                                           |
        // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
        // | Offset32 | extensionOffset     | Offset to the extension subtable, of lookup type extensionLookupType, relative to the start of the ExtensionSubstFormat1 subtable. |
        // +----------+---------------------+------------------------------------------------------------------------------------------------------------------------------------+
        ushort extensionLookupType = reader.ReadUInt16();
        uint extensionOffset = reader.ReadOffset32();

        // The extensionLookupType field must be set to any lookup type other than 7.
        // All subtables in a LookupType 7 lookup must have the same extensionLookupType.
        if (extensionLookupType == 7)
        {
            // Don't throw, we'll just ignore.
            return new NotImplementedSubTable();
        }

        // Read the lookup table again with the updated offset.
        return subTableLoader(extensionLookupType, lookupFlags, markFilteringSet, reader, offset + extensionOffset);
    }
}
