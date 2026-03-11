// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a CFF DICT operator with its name and operand kind.
/// Operators are registered in a static dictionary keyed by their byte encoding
/// and looked up during DICT parsing.
/// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5176.CFF.pdf"/>
/// </summary>
internal sealed class CFFOperator
{
    private static readonly Lazy<Dictionary<int, CFFOperator>> RegisteredOperators = new(CreateDictionary, true);

    private CFFOperator(string name, OperatorOperandKind operandKind)
    {
        this.Name = name;
        this.OperandKind = operandKind;
    }

    /// <summary>
    /// Gets the name of the operator (e.g. "CharStrings", "FontMatrix", "Private").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the expected operand format for this operator.
    /// </summary>
    public OperatorOperandKind OperandKind { get; }

    /// <summary>
    /// Looks up a registered CFF operator by its one- or two-byte encoding.
    /// </summary>
    /// <param name="b0">The first byte of the operator.</param>
    /// <param name="b1">The second byte (0 for single-byte operators, or 12 prefix byte value).</param>
    /// <returns>The matching <see cref="CFFOperator"/>, or <see langword="null"/> if not found.</returns>
    public static CFFOperator GetOperatorByKey(byte b0, byte b1)
    {
        RegisteredOperators.Value.TryGetValue((b1 << 8) | b0, out CFFOperator? found);
        return found!;
    }

    private static Dictionary<int, CFFOperator> CreateDictionary()
    {
        Dictionary<int, CFFOperator> dictionary = [];

        // Table 9: Top DICT Operator Entries
        Register(dictionary, 0, "version", OperatorOperandKind.SID);
        Register(dictionary, 1, "Notice", OperatorOperandKind.SID);
        Register(dictionary, 12, 0, "Copyright", OperatorOperandKind.SID);
        Register(dictionary, 2, "FullName", OperatorOperandKind.SID);
        Register(dictionary, 3, "FamilyName", OperatorOperandKind.SID);
        Register(dictionary, 4, "Weight", OperatorOperandKind.SID);
        Register(dictionary, 12, 1, "isFixedPitch", OperatorOperandKind.Boolean);
        Register(dictionary, 12, 2, "ItalicAngle", OperatorOperandKind.Number);
        Register(dictionary, 12, 3, "UnderlinePosition", OperatorOperandKind.Number);
        Register(dictionary, 12, 4, "UnderlineThickness", OperatorOperandKind.Number);
        Register(dictionary, 12, 5, "PaintType", OperatorOperandKind.Number);
        Register(dictionary, 12, 6, "CharstringType", OperatorOperandKind.Number); // default value 2
        Register(dictionary, 12, 7, "FontMatrix", OperatorOperandKind.Array);
        Register(dictionary, 13, "UniqueID", OperatorOperandKind.Number);
        Register(dictionary, 5, "FontBBox", OperatorOperandKind.Array);
        Register(dictionary, 12, 8, "StrokeWidth", OperatorOperandKind.Number);
        Register(dictionary, 14, "XUID", OperatorOperandKind.Array);
        Register(dictionary, 15, "charset", OperatorOperandKind.Number);
        Register(dictionary, 16, "Encoding", OperatorOperandKind.Number);
        Register(dictionary, 17, "CharStrings", OperatorOperandKind.Number);
        Register(dictionary, 18, "Private", OperatorOperandKind.NumberNumber);
        Register(dictionary, 12, 20, "SyntheticBase", OperatorOperandKind.Number);
        Register(dictionary, 12, 21, "PostScript", OperatorOperandKind.SID);
        Register(dictionary, 12, 22, "BaseFontName", OperatorOperandKind.SID);
        Register(dictionary, 12, 23, "BaseFontBlend", OperatorOperandKind.SID);

        // Table 10: CIDFont Operator Extensions
        Register(dictionary, 12, 30, "ROS", OperatorOperandKind.SID_SID_Number);
        Register(dictionary, 12, 31, "CIDFontVersion", OperatorOperandKind.Number);
        Register(dictionary, 12, 32, "CIDFontRevision", OperatorOperandKind.Number);
        Register(dictionary, 12, 33, "CIDFontType", OperatorOperandKind.Number);
        Register(dictionary, 12, 34, "CIDCount", OperatorOperandKind.Number);
        Register(dictionary, 12, 35, "UIDBase", OperatorOperandKind.Number);
        Register(dictionary, 12, 36, "FDArray", OperatorOperandKind.Number);
        Register(dictionary, 12, 37, "FDSelect", OperatorOperandKind.Number);
        Register(dictionary, 12, 38, "FontName", OperatorOperandKind.SID);

        // Table 23: Private DICT Operators
        Register(dictionary, 6, "BlueValues", OperatorOperandKind.Delta);
        Register(dictionary, 7, "OtherBlues", OperatorOperandKind.Delta);
        Register(dictionary, 8, "FamilyBlues", OperatorOperandKind.Delta);
        Register(dictionary, 9, "FamilyOtherBlues", OperatorOperandKind.Delta);
        Register(dictionary, 12, 9, "BlueScale", OperatorOperandKind.Number);
        Register(dictionary, 12, 10, "BlueShift", OperatorOperandKind.Number);
        Register(dictionary, 12, 11, "BlueFuzz", OperatorOperandKind.Number);
        Register(dictionary, 10, "StdHW", OperatorOperandKind.Number);
        Register(dictionary, 11, "StdVW", OperatorOperandKind.Number);
        Register(dictionary, 12, 12, "StemSnapH", OperatorOperandKind.Delta);
        Register(dictionary, 12, 13, "StemSnapV", OperatorOperandKind.Delta);
        Register(dictionary, 12, 14, "ForceBold", OperatorOperandKind.Boolean);

        // reserved 12 15
        // reserved 12 16
        Register(dictionary, 12, 17, "LanguageGroup", OperatorOperandKind.Number);
        Register(dictionary, 12, 18, "ExpansionFactor", OperatorOperandKind.Number);
        Register(dictionary, 12, 19, "initialRandomSeed", OperatorOperandKind.Number);

        Register(dictionary, 19, "Subrs", OperatorOperandKind.Number);
        Register(dictionary, 20, "defaultWidthX", OperatorOperandKind.Number);
        Register(dictionary, 21, "nominalWidthX", OperatorOperandKind.Number);

        // CFF2 operators
        Register(dictionary, 22, "vsindex", OperatorOperandKind.Number);
        Register(dictionary, 23, "blend", OperatorOperandKind.Number);
        Register(dictionary, 24, "vstore", OperatorOperandKind.Number);

        return dictionary;
    }

    private static void Register(Dictionary<int, CFFOperator> dictionary, byte b0, byte b1, string name, OperatorOperandKind operandKind)
        => dictionary.Add((b1 << 8) | b0, new CFFOperator(name, operandKind));

    private static void Register(Dictionary<int, CFFOperator> dictionary, byte b0, string name, OperatorOperandKind operandKind)
        => dictionary.Add(b0, new CFFOperator(name, operandKind));

#if DEBUG
    public override string ToString() => this.Name;
#endif
}
