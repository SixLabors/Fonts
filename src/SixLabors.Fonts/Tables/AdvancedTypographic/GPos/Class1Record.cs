// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Represents a Class1Record used in Pair Adjustment Positioning Format 2 (class-based kerning).
/// Each Class1Record contains an array of Class2Records, one for each class in the second class definition table.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pair-adjustment-positioning-format-2-class-pair-adjustment"/>
/// </summary>
internal sealed class Class1Record
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Class1Record"/> class.
    /// </summary>
    /// <param name="class2Records">The array of Class2 records.</param>
    private Class1Record(Class2Record[] class2Records) => this.Class2Records = class2Records;

    /// <summary>
    /// Gets the array of Class2 records, ordered by classes in the second class definition table.
    /// </summary>
    public Class2Record[] Class2Records { get; }

    /// <summary>
    /// Loads the <see cref="Class1Record"/> from the specified reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="class2Count">The number of classes in the second class definition table.</param>
    /// <param name="valueFormat1">The value format for the first glyph.</param>
    /// <param name="valueFormat2">The value format for the second glyph.</param>
    /// <param name="parentBase">The absolute stream position of the parent table for resolving device offsets.</param>
    /// <returns>The loaded <see cref="Class1Record"/>.</returns>
    public static Class1Record Load(BigEndianBinaryReader reader, int class2Count, ValueFormat valueFormat1, ValueFormat valueFormat2, long parentBase = -1)
    {
        // +--------------+----------------------------+---------------------------------------------+
        // | Type         | Name                       | Description                                 |
        // +==============+============================+=============================================+
        // | Class2Record | class2Records[class2Count] | Array of Class2 records, ordered by classes |
        // |              |                            | in classDef2.                               |
        // +--------------+----------------------------+---------------------------------------------+
        var class2Records = new Class2Record[class2Count];
        for (int i = 0; i < class2Records.Length; i++)
        {
            class2Records[i] = new Class2Record(reader, valueFormat1, valueFormat2, parentBase);
        }

        return new Class1Record(class2Records);
    }
}
