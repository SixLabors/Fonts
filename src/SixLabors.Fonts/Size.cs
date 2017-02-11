using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public struct Size
    {
        public Size(float width, float height)
        {
            this.Width = width;
            this.Height = height;
        }

        public float Height { get; private set; }
        public float Width { get; private set; }
    }
}
