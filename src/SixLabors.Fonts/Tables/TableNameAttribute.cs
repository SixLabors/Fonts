// Copyright (c) Six Labors and contributors.
// Licensed under the GNU Affero General Public License, Version 3.

using System;

namespace SixLabors.Fonts.Tables
{
    internal sealed class TableNameAttribute : Attribute
    {
        public TableNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}