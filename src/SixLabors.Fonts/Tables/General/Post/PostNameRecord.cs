// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

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
