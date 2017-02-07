using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    internal class Point
    {
        public Point(short x, short y)
        {
            this.X = x;
            this.Y = y;
        }

        public short X { get; }

        public short Y { get; }
    }
}
