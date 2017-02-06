using System;

namespace SixLabors.Fonts.Exceptions
{
    public class FontException : Exception
    {
        public FontException(string message)
            : base(message)
        {
        }
    }
}