using System;

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Base class for exceptions thrown by this library.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class FontFamilyNotFoundException : FontException
    {
        /// <summary>
        /// The name of the font familiy we failed to find.
        /// </summary>
        public string FontFamily { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontException"/> class.
        /// </summary>
        /// <param name="family">The name of the missing font family.</param>
        public FontFamilyNotFoundException(string family)
            : base($"{family} could not be found")
        {
            this.FontFamily = family;
        }
    }
}