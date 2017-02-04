using System;

namespace SixLabors.Fonts.Tables
{
    internal class TableNameAttribute : Attribute
    {
        public TableNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}