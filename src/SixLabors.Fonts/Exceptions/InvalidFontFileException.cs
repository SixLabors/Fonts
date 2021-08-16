// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Exception font loading can throw if it encounters invalid data during font loading.
    /// </summary>
    /// <seealso cref="Exception" />
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
