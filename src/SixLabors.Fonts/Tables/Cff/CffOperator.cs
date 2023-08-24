// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    internal sealed class CFFOperator
    {
        private static readonly Lazy<Dictionary<int, CFFOperator>> RegisteredOperators = new(() => CreateDictionary());
        private readonly byte b0;
        private readonly byte b1;
        private readonly OperatorOperandKind operatorOperandKind;

        // b0 the first byte of a two byte value
        // b1 the second byte of a two byte value
        private CFFOperator(string name, byte b0, byte b1, OperatorOperandKind operatorOperandKind)
        {
            this.b0 = b0;
            this.b1 = b1;
            this.Name = name;
            this.operatorOperandKind = operatorOperandKind;
        }

        public string Name { get; }

        public static CFFOperator GetOperatorByKey(byte b0, byte b1)
        {
            RegisteredOperators.Value.TryGetValue((b1 << 8) | b0, out CFFOperator? found);
            return found!;
        }

        private static Dictionary<int, CFFOperator> CreateDictionary()
        {
            Dictionary<int, CFFOperator> dictionary = new();

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

            // reserved 12 15//https://typekit.files.wordpress.com/2013/05/5176.cff.pdf
            // reserved 12 16//https://typekit.files.wordpress.com/2013/05/5176.cff.pdf
            Register(dictionary, 12, 17, "LanguageGroup", OperatorOperandKind.Number); // https://typekit.files.wordpress.com/2013/05/5176.cff.pdf
            Register(dictionary, 12, 18, "ExpansionFactor", OperatorOperandKind.Number); // https://typekit.files.wordpress.com/2013/05/5176.cff.pdf
            Register(dictionary, 12, 19, "initialRandomSeed", OperatorOperandKind.Number); // https://typekit.files.wordpress.com/2013/05/5176.cff.pdf

            Register(dictionary, 19, "Subrs", OperatorOperandKind.Number);
            Register(dictionary, 20, "defaultWidthX", OperatorOperandKind.Number);
            Register(dictionary, 21, "nominalWidthX", OperatorOperandKind.Number);

            return dictionary;
        }

        private static void Register(Dictionary<int, CFFOperator> dictionary, byte b0, byte b1, string operatorName, OperatorOperandKind opopKind)
            => dictionary.Add((b1 << 8) | b0, new CFFOperator(operatorName, b0, b1, opopKind));

        private static void Register(Dictionary<int, CFFOperator> dictionary, byte b0, string operatorName, OperatorOperandKind opopKind)
            => dictionary.Add(b0, new CFFOperator(operatorName, b0, 0, opopKind));

#if DEBUG
        public override string ToString() => this.Name;
#endif
    }
}
