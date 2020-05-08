// Copyright (c) Six Labors and contributors.
// Licensed under the GNU Affero General Public License, Version 3.

namespace SixLabors.Fonts.Tables
{
    internal sealed class UnknownTable : Table
    {
        internal UnknownTable(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}