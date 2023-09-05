// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Exception font loading can throw if it finds a required table is missing during font loading.
/// </summary>
/// <seealso cref="Exception" />
public class MissingFontTableException : InvalidFontFileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingFontTableException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="table">The table.</param>
    public MissingFontTableException(string message, string table)
        : base(message)
        => this.Table = table;

    /// <summary>
    /// Gets the table where the error originated.
    /// </summary>
    public string Table { get; }
}
