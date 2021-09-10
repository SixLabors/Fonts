// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Represents a unicode string and all associated attributes
    /// for each character required for the Bidi algorithm
    /// </summary>
    internal class BidiData
    {
        private ArrayBuilder<BidiCharacterType> types;
        private ArrayBuilder<BidiPairedBracketType> pairedBracketTypes;
        private ArrayBuilder<int> pairedBracketValues;
        private ArrayBuilder<BidiCharacterType> savedTypes;
        private ArrayBuilder<BidiPairedBracketType> savedPairedBracketTypes;
        private ArrayBuilder<sbyte> tempLevelBuffer;
        private readonly List<int> paragraphPositions = new();

        public sbyte ParagraphEmbeddingLevel { get; private set; }

        public bool HasBrackets { get; private set; }

        public bool HasEmbeddings { get; private set; }

        public bool HasIsolates { get; private set; }

        /// <summary>
        /// Gets the length of the data held by the BidiData
        /// </summary>
        public int Length => this.types.Length;

        /// <summary>
        /// Gets the BidiCharacterType of each code point
        /// </summary>
        public ReadOnlyArraySlice<BidiCharacterType> Types { get; private set; }

        /// <summary>
        /// Gets the paired bracket type for each code point
        /// </summary>
        public ReadOnlyArraySlice<BidiPairedBracketType> PairedBracketTypes { get; private set; }

        /// <summary>
        /// Gets the paired bracket value for code point
        /// </summary>
        /// <remarks>
        /// The paired bracket values are the code points
        /// of each character where the opening code point
        /// is replaced with the closing code point for easier
        /// matching.  Also, bracket code points are mapped
        /// to their canonical equivalents
        /// </remarks>
        public ReadOnlyArraySlice<int> PairedBracketValues { get; private set; }

        /// <summary>
        /// Initialize with a text value.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <param name="paragraphEmbeddingLevel">The paragraph embedding level</param>
        public void Init(ReadOnlySpan<char> text, sbyte paragraphEmbeddingLevel)
        {
            // Set working buffer sizes
            // TODO: This allocates more than it should for some arrays.
            int length = CodePoint.GetCodePointCount(text);
            this.types.Length = length;
            this.pairedBracketTypes.Length = length;
            this.pairedBracketValues.Length = length;

            this.paragraphPositions.Clear();
            this.ParagraphEmbeddingLevel = paragraphEmbeddingLevel;

            // Resolve the BidiCharacterType, paired bracket type and paired
            // bracket values for all code points
            this.HasBrackets = false;
            this.HasEmbeddings = false;
            this.HasIsolates = false;

            int i = 0;
            var codePointEnumerator = new SpanCodePointEnumerator(text);
            while (codePointEnumerator.MoveNext())
            {
                CodePoint codePoint = codePointEnumerator.Current;
                BidiClass bidi = CodePoint.GetBidiClass(codePoint);

                // Look up BidiCharacterType
                BidiCharacterType dir = bidi.CharacterType;
                this.types[i] = dir;

                switch (dir)
                {
                    case BidiCharacterType.LeftToRightEmbedding:
                    case BidiCharacterType.LeftToRightOverride:
                    case BidiCharacterType.RightToLeftEmbedding:
                    case BidiCharacterType.RightToLeftOverride:
                    case BidiCharacterType.PopDirectionalFormat:
                        this.HasEmbeddings = true;
                        break;

                    case BidiCharacterType.LeftToRightIsolate:
                    case BidiCharacterType.RightToLeftIsolate:
                    case BidiCharacterType.FirstStrongIsolate:
                    case BidiCharacterType.PopDirectionalIsolate:
                        this.HasIsolates = true;
                        break;
                }

                // Lookup paired bracket types
                BidiPairedBracketType pbt = bidi.PairedBracketType;
                this.pairedBracketTypes[i] = pbt;

                if (pbt == BidiPairedBracketType.Open)
                {
                    // Opening bracket types can never have a null pairing.
                    bidi.TryGetPairedBracket(out CodePoint paired);
                    this.pairedBracketValues[i] = BidiClass.MapCanonicalType(paired).Value;

                    this.HasBrackets = true;
                }
                else if (pbt == BidiPairedBracketType.Close)
                {
                    this.pairedBracketValues[i] = BidiClass.MapCanonicalType(codePoint).Value;
                    this.HasBrackets = true;
                }

                i++;
            }

            // Create slices on work buffers
            this.Types = this.types.AsSlice();
            this.PairedBracketTypes = this.pairedBracketTypes.AsSlice();
            this.PairedBracketValues = this.pairedBracketValues.AsSlice();
        }

        /// <summary>
        /// Save the Types and PairedBracketTypes of this bididata
        /// </summary>
        /// <remarks>
        /// This is used when processing embedded style runs with
        /// BidiCharacterType overrides.  TextLayout saves the data,
        /// overrides the style runs to neutral, processes the bidi
        /// data for the entire paragraph and then restores this data
        /// before processing the embedded runs.
        /// </remarks>
        public void SaveTypes()
        {
            // Capture the types data
            this.savedTypes.Clear();
            this.savedTypes.Add(this.types.AsSlice());
            this.savedPairedBracketTypes.Clear();
            this.savedPairedBracketTypes.Add(this.pairedBracketTypes.AsSlice());
        }

        /// <summary>
        /// Restore the data saved by SaveTypes
        /// </summary>
        public void RestoreTypes()
        {
            this.types.Clear();
            this.types.Add(this.savedTypes.AsSlice());
            this.pairedBracketTypes.Clear();
            this.pairedBracketTypes.Add(this.savedPairedBracketTypes.AsSlice());
        }

        /// <summary>
        /// Gets a temporary level buffer. Used by TextLayout when
        /// resolving style runs with different BidiCharacterType.
        /// </summary>
        /// <param name="length">Length of the required ExpandableBuffer</param>
        /// <returns>An uninitialized level ExpandableBuffer</returns>
        public ArraySlice<sbyte> GetTempLevelBuffer(int length)
        {
            this.tempLevelBuffer.Clear();
            return this.tempLevelBuffer.Add(length, false);
        }
    }
}
