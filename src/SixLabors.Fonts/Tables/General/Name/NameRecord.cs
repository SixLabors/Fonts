// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.Name;

/// <summary>
/// Represents a single name record entry in the OpenType 'name' table.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
/// </summary>
internal class NameRecord
{
    /// <summary>
    /// The resolved string value for this name record.
    /// </summary>
    private readonly string value;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameRecord"/> class.
    /// </summary>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="languageId">The language identifier.</param>
    /// <param name="nameId">The name identifier.</param>
    /// <param name="value">The string value of the name record.</param>
    public NameRecord(PlatformIDs platform, ushort languageId, KnownNameIds nameId, string value)
    {
        this.Platform = platform;
        this.LanguageID = languageId;
        this.NameID = nameId;
        this.value = value;
    }

    /// <summary>
    /// Gets the platform identifier for this name record.
    /// </summary>
    public PlatformIDs Platform { get; }

    /// <summary>
    /// Gets the platform-specific language identifier for this name record.
    /// </summary>
    public ushort LanguageID { get; }

    /// <summary>
    /// Gets the name identifier indicating what kind of name this record contains.
    /// </summary>
    public KnownNameIds NameID { get; }

    /// <summary>
    /// Gets the string loader used to lazily read the string value from the font data.
    /// </summary>
    internal StringLoader? StringReader { get; private set; }

    /// <summary>
    /// Gets the resolved string value for this name record.
    /// </summary>
    public string Value => this.StringReader?.Value ?? this.value;

    /// <summary>
    /// Reads a <see cref="NameRecord"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the name record.</param>
    /// <returns>The parsed <see cref="NameRecord"/>.</returns>
    public static NameRecord Read(BigEndianBinaryReader reader)
    {
        PlatformIDs platform = reader.ReadUInt16<PlatformIDs>();
        EncodingIDs encodingId = reader.ReadUInt16<EncodingIDs>();
        Encoding encoding = encodingId.AsEncoding();
        ushort languageID = reader.ReadUInt16();
        KnownNameIds nameID = reader.ReadUInt16<KnownNameIds>();

        StringLoader stringReader = StringLoader.Create(reader, encoding);

        return new NameRecord(platform, languageID, nameID, string.Empty)
        {
            StringReader = stringReader
        };
    }
}
