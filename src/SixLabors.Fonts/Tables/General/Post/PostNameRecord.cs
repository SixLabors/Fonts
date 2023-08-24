// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Post
{
    internal class PostNameRecord
    {
        internal PostNameRecord(ushort nameIndex, string name)
        {
            this.Name = name;
            this.NameIndex = nameIndex;
        }

        public ushort NameIndex { get; }

        public string Name { get; }
    }
}
