using System;

namespace SixLabors.Fonts.Exceptions
{
    public class InvalidFontFileException : Exception
    {
        public InvalidFontFileException(string message)
            : base(message)
        {
        }
    }
}