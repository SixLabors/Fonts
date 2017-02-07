using System;

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Exception font loading can throw if it encounteres invalid data during font loading.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class InvalidFontFileException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidFontFileException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidFontFileException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Exception font loading can throw if it encounteres invalid data during font loading.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class InvalidFontTableException : InvalidFontFileException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidFontFileException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidFontTableException(string message, string table)
            : base(message)
        {
            this.Table = table;
        }

        public string Table { get; }
    }
}