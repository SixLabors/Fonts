// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

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
}