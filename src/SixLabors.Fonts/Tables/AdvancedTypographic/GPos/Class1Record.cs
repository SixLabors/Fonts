// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

internal sealed class Class1Record
{
    private Class1Record(Class2Record[] class2Records) => this.Class2Records = class2Records;

    public Class2Record[] Class2Records { get; }

    public static Class1Record Load(BigEndianBinaryReader reader, int class2Count, ValueFormat valueFormat1, ValueFormat valueFormat2)
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
            class2Records[i] = new Class2Record(reader, valueFormat1, valueFormat2);
        }

        return new Class1Record(class2Records);
    }
}
