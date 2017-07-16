// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Base class for exceptions thrown by this library.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class FontException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FontException(string message)
            : base(message)
        {
        }
    }
}