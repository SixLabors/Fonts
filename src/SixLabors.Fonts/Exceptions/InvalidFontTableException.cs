using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Exception font loading can throw if it encounteres invalid data during font loading.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class InvalidFontTableException : InvalidFontFileException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidFontFileException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="table">The table.</param>
        public InvalidFontTableException(string message, string table)
            : base(message)
        {
            this.Table = table;
        }

        /// <summary>
        /// Gets the table where the error originated.
        /// </summary>
        public string Table { get; }
    }
}
