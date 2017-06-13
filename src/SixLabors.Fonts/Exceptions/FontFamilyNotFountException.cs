using System;

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Base class for exceptions thrown by this library.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class FontFamilyNotFountException : FontException
    {
        public string FontFamily { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FontFamilyNotFountException(string family)
            : base($"{family} could not be found")
        {
            this.FontFamily = family;
        }
    }
}