// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        private readonly List<int> paragraphPositions = new List<int>();

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
        public ArraySlice<BidiCharacterType> Types { get; private set; }

        /// <summary>
        /// Gets the paired bracket type for each code point
        /// </summary>
        public ArraySlice<BidiPairedBracketType> PairedBracketTypes { get; private set; }

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
        public ArraySlice<int> PairedBracketValues { get; private set; }

        /// <summary>
        /// Initialize with an array of Unicode code points
        /// </summary>
        /// <param name="codePoints">The unicode code points to be processed</param>
        /// <param name="paragraphEmbeddingLevel">The paragraph embedding level</param>
        public void Init(ArraySlice<int> codePoints, sbyte paragraphEmbeddingLevel)
        {
            // Set working buffer sizes
            this.types.Length = codePoints.Length;
            this.pairedBracketTypes.Length = codePoints.Length;
            this.pairedBracketValues.Length = codePoints.Length;

            this.paragraphPositions.Clear();
            this.ParagraphEmbeddingLevel = paragraphEmbeddingLevel;

            // Resolve the BidiCharacterType, paired bracket type and paired bracket values for
            // all code points
            this.HasBrackets = false;
            this.HasEmbeddings = false;
            this.HasIsolates = false;
            for (int i = 0; i < codePoints.Length; i++)
            {
                var bidiData = UnicodeData.GetBidiData(codePoints[i]);

                // Look up BidiCharacterType
                var dir = (BidiCharacterType)(bidiData >> 24);
                this.types[i] = dir;

                switch (dir)
                {
                    case BidiCharacterType.LRE:
                    case BidiCharacterType.LRO:
                    case BidiCharacterType.RLE:
                    case BidiCharacterType.RLO:
                    case BidiCharacterType.PDF:
                        this.HasEmbeddings = true;
                        break;

                    case BidiCharacterType.LRI:
                    case BidiCharacterType.RLI:
                    case BidiCharacterType.FSI:
                    case BidiCharacterType.PDI:
                        this.HasIsolates = true;
                        break;
                }

                // Lookup paired bracket types
                var pbt = (BidiPairedBracketType)((bidiData >> 16) & 0xFF);
                this.pairedBracketTypes[i] = pbt;
                switch (pbt)
                {
                    case BidiPairedBracketType.O:
                        this.pairedBracketValues[i] = MapCanon((int)(bidiData & 0xFFFF));
                        this.HasBrackets = true;
                        break;

                    case BidiPairedBracketType.C:
                        this.pairedBracketValues[i] = MapCanon(codePoints[i]);
                        this.HasBrackets = true;
                        break;
                }
            }

            // Create slices on work buffers
            this.Types = this.types.AsSlice();
            this.PairedBracketTypes = this.pairedBracketTypes.AsSlice();
            this.PairedBracketValues = this.pairedBracketValues.AsSlice();
        }

        /// <summary>
        /// Map bracket types 0x3008 and 0x3009 to their canonical equivalents
        /// </summary>
        /// <param name="codePoint">The code point to be mapped</param>
        /// <returns>The mapped canonical code point, or the passed code point</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MapCanon(int codePoint)
        {
            if (codePoint == 0x3008)
            {
                return 0x2329;
            }

            if (codePoint == 0x3009)
            {
                return 0x232A;
            }
            else
            {
                return codePoint;
            }
        }

        /// <summary>
        /// Save the Types and PairedBracketTypes of this bididata
        /// </summary>
        /// <remarks>
        /// This is used when processing embedded style runs with
        /// BidiCharacterType overrides.  TextBlock saves the data,
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
        /// Gets a temporary level buffer.  Used by TextBlock when
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
