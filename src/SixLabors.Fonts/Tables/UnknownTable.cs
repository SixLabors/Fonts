using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tables
{
    using System.IO;
    using System.Text;

    internal class UnknownTable : Table
    {
        internal UnknownTable(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}