// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Implementation of Unicode Bidirection Algorithm (UAX #9)
    /// https://unicode.org/reports/tr9/
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Bidi algorithm uses a number of memory arrays for resolved
    /// types, level information, bracket types, x9 removal maps and
    /// more...
    /// </para>
    /// <para>
    /// This implementation of the Bidi algorithm has been designed
    /// to reduce memory pressure on the GC by re-using the same
    /// work buffers, so instances of this class should be re-used
    /// as much as possible.
    /// </para>
    /// </remarks>
    internal sealed class Bidi
    {
        /// <summary>
        /// The original BidiCharacterType types as provided by the caller
        /// </summary>
        private BufferSlice<BidiCharacterType> originalTypes;

        /// <summary>
        /// Paired bracket types as provided by caller
        /// </summary>
        private BufferSlice<BidiPairedBracketType> pairedBracketTypes;

        /// <summary>
        /// Paired bracket values as provided by caller
        /// </summary>
        private BufferSlice<int> pairedBracketValues;

        /// <summary>
        /// Try if the incoming data is known to contain brackets
        /// </summary>
        private bool hasBrackets;

        /// <summary>
        /// True if the incoming data is known to contain embedding runs
        /// </summary>
        private bool hasEmbeddings;

        /// <summary>
        /// True if the incoming data is known to contain isolating runs
        /// </summary>
        private bool hasIsolates;

        /// <summary>
        /// Two directional mapping of isolate start/end pairs
        /// </summary>
        /// <remarks>
        /// The forward mapping maps the start index to the end index.
        /// The reverse mapping maps the end index to the start index.
        /// </remarks>
        private readonly BidiDictionary<int, int> isolatePairs = new BidiDictionary<int, int>();

        /// <summary>
        /// The working BidiCharacterType types
        /// </summary>
        private BufferSlice<BidiCharacterType> workingTypes;

        /// <summary>
        /// The buffer underlying _workingTypes
        /// </summary>
        private ExpandableBuffer<BidiCharacterType> workingTypesBuffer;

        /// <summary>
        /// The resolved levels
        /// </summary>
        private BufferSlice<sbyte> resolvedLevels;

        /// <summary>
        /// The buffer underlying _resolvedLevels
        /// </summary>
        private ExpandableBuffer<sbyte> resolvedLevelsBuffer;

        /// <summary>
        /// The resolve paragraph embedding level
        /// </summary>
        private sbyte paragraphEmbeddingLevel;

        /// <summary>
        /// The status stack used during resolution of explicit
        /// embedding and isolating runs
        /// </summary>
        private readonly Stack<Status> statusStack = new Stack<Status>();

        /// <summary>
        /// Mapping used to virtually remove characters for rule X9
        /// </summary>
        private ExpandableBuffer<int> x9Map;

        /// <summary>
        /// Re-usable list of level runs
        /// </summary>
        private readonly List<LevelRun> levelRuns = new List<LevelRun>();

        /// <summary>
        /// Mapping for the current isolating sequence, built
        /// by joining level runs from the x9 map.
        /// </summary>
        private ExpandableBuffer<int> isolatedRunMapping;

        /// <summary>
        /// A stack of pending isolate openings used by FindIsolatePairs()
        /// </summary>
        private readonly Stack<int> pendingIsolateOpenings = new Stack<int>();

        /// <summary>
        /// The level of the isolating run currently being processed
        /// </summary>
        private int runLevel;

        /// <summary>
        /// The direction of the isolating run currently being processed
        /// </summary>
        private BidiCharacterType runDirection;

        /// <summary>
        /// The length of the isolating run currently being processed
        /// </summary>
        private int runLength;

        /// <summary>
        /// A mapped slice of the resolved types for the isolating run currently
        /// being processed
        /// </summary>
        private MappedBuffer<BidiCharacterType> runResolvedTypes;

        /// <summary>
        /// A mapped slice of the original types for the isolating run currently
        /// being processed
        /// </summary>
        private MappedBuffer<BidiCharacterType> runOriginalTypes;

        /// <summary>
        /// A mapped slice of the run levels for the isolating run currently
        /// being processed
        /// </summary>
        private MappedBuffer<sbyte> runLevels;

        /// <summary>
        /// A mapped slice of the paired bracket types of the isolating
        /// run currently being processed
        /// </summary>
        private MappedBuffer<BidiPairedBracketType> runBidiPairedBracketTypes;

        /// <summary>
        /// A mapped slice of the paired bracket values of the isolating
        /// run currently being processed
        /// </summary>
        private MappedBuffer<int> runPairedBracketValues;

        /// <summary>
        /// Maximum pairing depth for paired brackets
        /// </summary>
        private const int MaxPairedBracketDepth = 63;

        /// <summary>
        /// Re-useable list of pending opening brackets used by the
        /// LocatePairedBrackets method
        /// </summary>
        private readonly List<int> pendingOpeningBrackets = new List<int>();

        /// <summary>
        /// Resolved list of paired brackets
        /// </summary>
        private readonly List<BracketPair> pairedBrackets = new List<BracketPair>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Bidi"/> class.
        /// </summary>
        public Bidi()
        {
        }

        /// <summary>
        /// Gets a per-thread instance that can be re-used as often
        /// as necessary.
        /// </summary>
        public static ThreadLocal<Bidi> Instance { get; } = new ThreadLocal<Bidi>(() => new Bidi());

        /// <summary>
        /// Gets the resolved levels
        /// </summary>
        public BufferSlice<sbyte> ResolvedLevels => this.resolvedLevels;

        /// <summary>
        /// Gets the resolved paragraph embedding level
        /// </summary>
        public int ResolvedParagraphEmbeddingLevel => this.paragraphEmbeddingLevel;

        /// <summary>
        /// Process data from a BidiData instance
        /// </summary>
        /// <param name="data"></param>
        //public void Process(BidiData data)
        //{
        //    Process(
        //        data.Types,
        //        data.BidiPairedBracketTypes,
        //        data.PairedBracketValues,
        //        data.ParagraphEmbeddingLevel,
        //        data.HasBrackets,
        //        data.HasEmbeddings,
        //        data.HasIsolates, null);
        //}

        /// <summary>
        /// Processes Bidi Data
        /// </summary>
        public void Process(
            BufferSlice<BidiCharacterType> types,
            BufferSlice<BidiPairedBracketType> pairedBracketTypes,
            BufferSlice<int> pairedBracketValues,
            sbyte paragraphEmbeddingLevel,
            bool? hasBrackets,
            bool? hasEmbeddings,
            bool? hasIsolates,
            BufferSlice<sbyte>? outLevels)
        {
            // Reset state
            this.isolatePairs.Clear();
            this.workingTypesBuffer.Clear();
            this.levelRuns.Clear();
            this.resolvedLevelsBuffer.Clear();

            // Setup original types and working types
            this.originalTypes = types;
            this.workingTypes = this.workingTypesBuffer.Add(types);

            // Capture paired bracket values and types
            this.pairedBracketTypes = pairedBracketTypes;
            this.pairedBracketValues = pairedBracketValues;

            // Store things we know
            this.hasBrackets = hasBrackets ?? this.pairedBracketTypes.Length == this.originalTypes.Length;
            this.hasEmbeddings = hasEmbeddings ?? true;
            this.hasIsolates = hasIsolates ?? true;

            // Find all isolate pairs
            this.FindIsolatePairs();

            // Resolve the paragraph embedding level
            if (paragraphEmbeddingLevel == 2)
            {
                this.paragraphEmbeddingLevel = this.ResolveEmbeddingLevel(this.originalTypes);
            }
            else
            {
                this.paragraphEmbeddingLevel = paragraphEmbeddingLevel;
            }

            // Create resolved levels buffer
            if (outLevels.HasValue)
            {
                if (outLevels.Value.Length != this.originalTypes.Length)
                {
                    throw new ArgumentException("Out levels must be the same length as the input data");
                }

                this.resolvedLevels = outLevels.Value;
            }
            else
            {
                this.resolvedLevels = this.resolvedLevelsBuffer.Add(this.originalTypes.Length);
                this.resolvedLevels.Fill(this.paragraphEmbeddingLevel);
            }

            // Resolve explicit embedding levels (Rules X1-X8)
            this.ResolveExplicitEmbeddingLevels();

            // Build the rule X9 map
            this.BuildX9RemovalMap();

            // Process all isolated run sequences
            this.ProcessIsolatedRunSequences();

            // Reset whitespace levels
            this.ResetWhitespaceLevels();

            // Clean up
            this.AssignLevelsToCodePointsRemovedByX9();
        }

        /// <summary>
        /// Resolve the paragraph embedding level if not explicitly passed
        /// by the caller. Also used by rule X5c for FSI isolating sequences.
        /// </summary>
        /// <param name="data">The data to be evaluated</param>
        /// <returns>The resolved embedding level</returns>
        public sbyte ResolveEmbeddingLevel(BufferSlice<BidiCharacterType> data)
        {
            // P2
            for (int i = 0; i < data.Length; ++i)
            {
                switch (data[i])
                {
                    case BidiCharacterType.L:
                        // P3
                        return 0;

                    case BidiCharacterType.AL:
                    case BidiCharacterType.R:
                        // P3
                        return 1;

                    case BidiCharacterType.FSI:
                    case BidiCharacterType.LRI:
                    case BidiCharacterType.RLI:
                        // Skip isolate pairs
                        // (Because we're working with a slice, we need to adjust the indicies
                        //  we're using for the isolatePairs map)
                        if (this.isolatePairs.TryGetValue(data.Start + i, out i))
                        {
                            i -= data.Start;
                        }
                        else
                        {
                            i = data.Length;
                        }

                        break;
                }
            }

            // P3
            return 0;
        }

        /// <summary>
        /// Build a list of matching isolates for a directionality slice
        /// Implements BD9
        /// </summary>
        private void FindIsolatePairs()
        {
            // Redundant?
            if (!this.hasIsolates)
            {
                return;
            }

            // Lets double check this as we go and clear the flag
            // if there actually aren't any isolate pairs as this might
            // mean we can skip some later steps
            this.hasIsolates = false;

            // BD9...
            this.pendingIsolateOpenings.Clear();
            for (int i = 0; i < this.originalTypes.Length; i++)
            {
                BidiCharacterType t = this.originalTypes[i];
                if (t == BidiCharacterType.LRI || t == BidiCharacterType.RLI || t == BidiCharacterType.FSI)
                {
                    this.pendingIsolateOpenings.Push(i);
                    this.hasIsolates = true;
                }
                else if (t == BidiCharacterType.PDI)
                {
                    if (this.pendingIsolateOpenings.Count > 0)
                    {
                        this.isolatePairs.Add(this.pendingIsolateOpenings.Pop(), i);
                    }

                    this.hasIsolates = true;
                }
            }
        }

        /// <summary>
        /// Resolve the explicit embedding levels from the original
        /// data.  Implements rules X1 to X8.
        /// </summary>
        private void ResolveExplicitEmbeddingLevels()
        {
            // Redundant?
            if (!this.hasIsolates && !this.hasEmbeddings)
            {
                return;
            }

            // Work variables
            this.statusStack.Clear();
            int overflowIsolateCount = 0;
            int overflowEmbeddingCount = 0;
            int validIsolateCount = 0;

            // Constants
            const int maxStackDepth = 125;

            // Rule X1 - setup initial state
            this.statusStack.Clear();

            // Neutral
            this.statusStack.Push(new Status(this.paragraphEmbeddingLevel, BidiCharacterType.ON, false));

            // Process all characters
            for (int i = 0; i < this.originalTypes.Length; i++)
            {
                switch (this.originalTypes[i])
                {
                    case BidiCharacterType.RLE:
                    {
                        // Rule X2
                        sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 1) | 1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            this.statusStack.Push(new Status(newLevel, BidiCharacterType.ON, false));
                            this.resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiCharacterType.LRE:
                    {
                        // Rule X3
                        sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 2) & ~1);
                        if (newLevel < maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            this.statusStack.Push(new Status(newLevel, BidiCharacterType.ON, false));
                            this.resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiCharacterType.RLO:
                    {
                        // Rule X4
                        sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 1) | 1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            this.statusStack.Push(new Status(newLevel, BidiCharacterType.R, false));
                            this.resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiCharacterType.LRO:
                    {
                        // Rule X5
                        sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 2) & ~1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            this.statusStack.Push(new Status(newLevel, BidiCharacterType.L, false));
                            this.resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiCharacterType.RLI:
                    case BidiCharacterType.LRI:
                    case BidiCharacterType.FSI:
                    {
                        // Rule X5a, X5b and X5c
                        BidiCharacterType resolvedIsolate = this.originalTypes[i];

                        if (resolvedIsolate == BidiCharacterType.FSI)
                        {
                            if (!this.isolatePairs.TryGetValue(i, out int endOfIsolate))
                            {
                                endOfIsolate = this.originalTypes.Length;
                            }

                            // Rule X5c
                            if (this.ResolveEmbeddingLevel(this.originalTypes.Slice(i + 1, endOfIsolate - (i + 1))) == 1)
                            {
                                resolvedIsolate = BidiCharacterType.RLI;
                            }
                            else
                            {
                                resolvedIsolate = BidiCharacterType.LRI;
                            }
                        }

                        // Replace RLI's level with current embedding level
                        Status tos = this.statusStack.Peek();
                        this.resolvedLevels[i] = tos.EmbeddingLevel;

                        // Apply override
                        if (tos.OverrideStatus != BidiCharacterType.ON)
                        {
                            this.workingTypes[i] = tos.OverrideStatus;
                        }

                        // Work out new level
                        sbyte newLevel;
                        if (resolvedIsolate == BidiCharacterType.RLI)
                        {
                            newLevel = (sbyte)((tos.EmbeddingLevel + 1) | 1);
                        }
                        else
                        {
                            newLevel = (sbyte)((tos.EmbeddingLevel + 2) & ~1);
                        }

                        // Valid?
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            validIsolateCount++;
                            this.statusStack.Push(new Status(newLevel, BidiCharacterType.ON, true));
                        }
                        else
                        {
                            overflowIsolateCount++;
                        }

                        break;
                    }

                    case BidiCharacterType.BN:
                    {
                        // Mentioned in rule X6 - "for all types besides ..., BN, ..."
                        // no-op
                        break;
                    }

                    default:
                    {
                        // Rule X6
                        Status tos = this.statusStack.Peek();
                        this.resolvedLevels[i] = tos.EmbeddingLevel;
                        if (tos.OverrideStatus != BidiCharacterType.ON)
                        {
                            this.workingTypes[i] = tos.OverrideStatus;
                        }

                        break;
                    }

                    case BidiCharacterType.PDI:
                    {
                        // Rule X6a
                        if (overflowIsolateCount > 0)
                        {
                            overflowIsolateCount--;
                        }
                        else if (validIsolateCount != 0)
                        {
                            overflowEmbeddingCount = 0;
                            while (!this.statusStack.Peek().IsolateStatus)
                            {
                                this.statusStack.Pop();
                            }

                            this.statusStack.Pop();
                            validIsolateCount--;
                        }

                        Status tos = this.statusStack.Peek();
                        this.resolvedLevels[i] = tos.EmbeddingLevel;
                        if (tos.OverrideStatus != BidiCharacterType.ON)
                        {
                            this.workingTypes[i] = tos.OverrideStatus;
                        }

                        break;
                    }

                    case BidiCharacterType.PDF:
                    {
                        // Rule X7
                        if (overflowIsolateCount == 0)
                        {
                            if (overflowEmbeddingCount > 0)
                            {
                                overflowEmbeddingCount--;
                            }
                            else if (!this.statusStack.Peek().IsolateStatus && this.statusStack.Count >= 2)
                            {
                                this.statusStack.Pop();
                            }
                        }

                        break;
                    }

                    case BidiCharacterType.B:
                    {
                        // Rule X8
                        this.resolvedLevels[i] = this.paragraphEmbeddingLevel;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Build a map to the original data positions that excludes all
        /// the types defined by rule X9
        /// </summary>
        private void BuildX9RemovalMap()
        {
            // Reserve room for the x9 map
            this.x9Map.Length = this.originalTypes.Length;

            if (this.hasEmbeddings || this.hasIsolates)
            {
                // Build a map the removes all x9 characters
                int j = 0;
                for (int i = 0; i < this.originalTypes.Length; i++)
                {
                    if (!IsRemovedByX9(this.originalTypes[i]))
                    {
                        this.x9Map[j++] = i;
                    }
                }

                // Set the final length
                this.x9Map.Length = j;
            }
            else
            {
                for (int i = 0, count = this.originalTypes.Length; i < count; i++)
                {
                    this.x9Map[i] = i;
                }
            }
        }

        /// <summary>
        /// Find the original character index for an entry in the X9 map
        /// </summary>
        /// <param name="index">Index in the x9 removal map</param>
        /// <returns>Index to the original data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int MapX9(int index) => this.x9Map[index];

        /// <summary>
        /// Add a new level run
        /// </summary>
        /// <remarks>
        /// This method resolves the sos and eos values for the run
        /// and adds the run to the list
        /// /// </remarks>
        /// <param name="start">The index of the start of the run (in x9 removed units)</param>
        /// <param name="length">The length of the run (in x9 removed units)</param>
        /// <param name="level">The level of the run</param>
        private void AddLevelRun(int start, int length, int level)
        {
            // Get original indicies to first and last character in this run
            int firstCharIndex = this.MapX9(start);
            int lastCharIndex = this.MapX9(start + length - 1);

            // Work out sos
            int i = firstCharIndex - 1;
            while (i >= 0 && IsRemovedByX9(this.originalTypes[i]))
            {
                i--;
            }

            sbyte prevLevel = i < 0 ? this.paragraphEmbeddingLevel : this.resolvedLevels[i];
            BidiCharacterType sos = DirectionFromLevel(Math.Max(prevLevel, level));

            // Work out eos
            BidiCharacterType lastType = this.workingTypes[lastCharIndex];
            int nextLevel;
            if (lastType == BidiCharacterType.LRI || lastType == BidiCharacterType.RLI || lastType == BidiCharacterType.FSI)
            {
                nextLevel = this.paragraphEmbeddingLevel;
            }
            else
            {
                i = lastCharIndex + 1;
                while (i < this.originalTypes.Length && IsRemovedByX9(this.originalTypes[i]))
                {
                    i++;
                }

                nextLevel = i >= this.originalTypes.Length ? this.paragraphEmbeddingLevel : this.resolvedLevels[i];
            }

            BidiCharacterType eos = DirectionFromLevel(Math.Max(nextLevel, level));

            // Add the run
            this.levelRuns.Add(new LevelRun(start, length, level, sos, eos));
        }

        /// <summary>
        /// Find all runs of the same level, populating the _levelRuns
        /// collection
        /// </summary>
        private void FindLevelRuns()
        {
            int currentLevel = -1;
            int runStart = 0;
            for (int i = 0; i < this.x9Map.Length; ++i)
            {
                int level = this.resolvedLevels[this.MapX9(i)];
                if (level != currentLevel)
                {
                    if (currentLevel != -1)
                    {
                        this.AddLevelRun(runStart, i - runStart, currentLevel);
                    }

                    currentLevel = level;
                    runStart = i;
                }
            }

            // Don't forget the final level run
            if (currentLevel != -1)
            {
                this.AddLevelRun(runStart, this.x9Map.Length - runStart, currentLevel);
            }
        }

        /// <summary>
        /// Given a character index, find the level run that starts at that position
        /// </summary>
        /// <param name="index">The index into the original (unmapped) data</param>
        /// <returns>The index of the run that starts at that index</returns>
        private int FindRunForIndex(int index)
        {
            for (int i = 0; i < this.levelRuns.Count; i++)
            {
                // Passed index is for the original non-x9 filtered data, however
                // the level run ranges are for the x9 filtered data.  Convert before
                // comparing
                if (this.MapX9(this.levelRuns[i].Start) == index)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("Internal error");
        }

        /// <summary>
        /// Determine and the process all isolated run sequences
        /// </summary>
        private void ProcessIsolatedRunSequences()
        {
            // Find all runs with the same level
            this.FindLevelRuns();

            // Process them one at a time by first building
            // a mapping using slices from the x9 map for each
            // run section that needs to be joined together to
            // form an complete run.  That full run mapping
            // will be placed in _isolatedRunMapping and then
            // processed by ProcessIsolatedRunSequence().
            while (this.levelRuns.Count > 0)
            {
                // Clear the mapping
                this.isolatedRunMapping.Clear();

                // Combine mappings from this run and all runs that continue on from it
                int runIndex = 0;
                BidiCharacterType eos = this.levelRuns[0].Eos;
                BidiCharacterType sos = this.levelRuns[0].Sos;
                int level = this.levelRuns[0].Level;
                while (true)
                {
                    // Get the run
                    LevelRun r = this.levelRuns[runIndex];

                    // The eos of the isolating run is the eos of the
                    // last level run that comprises it.
                    eos = r.Eos;

                    // Remove this run as we've now processed it
                    this.levelRuns.RemoveAt(runIndex);

                    // Add the x9 map indicies for the run range to the mapping
                    // for this isolated run
                    this.isolatedRunMapping.Add(this.x9Map.Slice(r.Start, r.Length));

                    // Get the last character and see if it's an isolating run with a matching
                    // PDI and concatenate that run to this one
                    int lastCharacterIndex = this.isolatedRunMapping[this.isolatedRunMapping.Length - 1];
                    BidiCharacterType lastType = this.originalTypes[lastCharacterIndex];
                    if ((lastType == BidiCharacterType.LRI || lastType == BidiCharacterType.RLI || lastType == BidiCharacterType.FSI) &&
                            this.isolatePairs.TryGetValue(lastCharacterIndex, out int nextRunIndex))
                    {
                        // Find the continuing run index
                        runIndex = this.FindRunForIndex(nextRunIndex);
                    }
                    else
                    {
                        break;
                    }
                }

                // Process this isolated run
                this.ProcessIsolatedRunSequence(sos, eos, level);
            }
        }

        /// <summary>
        /// Process a single isolated run sequence, where the character sequence
        /// mapping is currently held in _isolatedRunMapping.
        /// </summary>
        private void ProcessIsolatedRunSequence(BidiCharacterType sos, BidiCharacterType eos, int runLevel)
        {
            // Create mappings onto the underlying data
            this.runResolvedTypes = new MappedBuffer<BidiCharacterType>(this.workingTypes, this.isolatedRunMapping.AsSlice());
            this.runOriginalTypes = new MappedBuffer<BidiCharacterType>(this.originalTypes, this.isolatedRunMapping.AsSlice());
            this.runLevels = new MappedBuffer<sbyte>(this.resolvedLevels, this.isolatedRunMapping.AsSlice());
            if (this.hasBrackets)
            {
                this.runBidiPairedBracketTypes = new MappedBuffer<BidiPairedBracketType>(this.pairedBracketTypes, this.isolatedRunMapping.AsSlice());
                this.runPairedBracketValues = new MappedBuffer<int>(this.pairedBracketValues, this.isolatedRunMapping.AsSlice());
            }

            this.runLevel = runLevel;
            this.runDirection = DirectionFromLevel(runLevel);
            this.runLength = this.runResolvedTypes.Length;

            // By tracking the types of characters known to be in the current run, we can
            // skip some of the rules that we know won't apply.  The flags will be
            // initialized while we're processing rule W1 below.
            bool hasEN = false;
            bool hasAL = false;
            bool hasES = false;
            bool hasCS = false;
            bool hasAN = false;
            bool hasET = false;

            // Rule W1
            // Also, set hasXX flags
            int i;
            BidiCharacterType prevType = sos;
            for (i = 0; i < this.runLength; i++)
            {
                BidiCharacterType t = this.runResolvedTypes[i];
                switch (t)
                {
                    case BidiCharacterType.NSM:
                        this.runResolvedTypes[i] = prevType;
                        break;

                    case BidiCharacterType.LRI:
                    case BidiCharacterType.RLI:
                    case BidiCharacterType.FSI:
                    case BidiCharacterType.PDI:
                        prevType = BidiCharacterType.ON;
                        break;

                    case BidiCharacterType.EN:
                        hasEN = true;
                        prevType = t;
                        break;

                    case BidiCharacterType.AL:
                        hasAL = true;
                        prevType = t;
                        break;

                    case BidiCharacterType.ES:
                        hasES = true;
                        prevType = t;
                        break;

                    case BidiCharacterType.CS:
                        hasCS = true;
                        prevType = t;
                        break;

                    case BidiCharacterType.AN:
                        hasAN = true;
                        prevType = t;
                        break;

                    case BidiCharacterType.ET:
                        hasET = true;
                        prevType = t;
                        break;

                    default:
                        prevType = t;
                        break;
                }
            }

            // Rule W2
            if (hasEN)
            {
                for (i = 0; i < this.runLength; i++)
                {
                    if (this.runResolvedTypes[i] == BidiCharacterType.EN)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            BidiCharacterType t = this.runResolvedTypes[j];
                            if (t == BidiCharacterType.L || t == BidiCharacterType.R || t == BidiCharacterType.AL)
                            {
                                if (t == BidiCharacterType.AL)
                                {
                                    this.runResolvedTypes[i] = BidiCharacterType.AN;
                                    hasAN = true;
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // Rule W3
            if (hasAL)
            {
                for (i = 0; i < this.runLength; i++)
                {
                    if (this.runResolvedTypes[i] == BidiCharacterType.AL)
                    {
                        this.runResolvedTypes[i] = BidiCharacterType.R;
                    }
                }
            }

            // Rule W4
            if ((hasES || hasCS) && (hasEN || hasAN))
            {
                for (i = 1; i < this.runLength - 1; ++i)
                {
                    ref BidiCharacterType rt = ref this.runResolvedTypes[i];
                    if (rt == BidiCharacterType.ES)
                    {
                        BidiCharacterType prevSepType = this.runResolvedTypes[i - 1];
                        BidiCharacterType succSepType = this.runResolvedTypes[i + 1];

                        if (prevSepType == BidiCharacterType.EN && succSepType == BidiCharacterType.EN)
                        {
                            // ES between EN and EN
                            rt = BidiCharacterType.EN;
                        }
                    }
                    else if (rt == BidiCharacterType.CS)
                    {
                        BidiCharacterType prevSepType = this.runResolvedTypes[i - 1];
                        BidiCharacterType succSepType = this.runResolvedTypes[i + 1];

                        if ((prevSepType == BidiCharacterType.AN && succSepType == BidiCharacterType.AN) ||
                             (prevSepType == BidiCharacterType.EN && succSepType == BidiCharacterType.EN))
                        {
                            // CS between (AN and AN) or (EN and EN)
                            rt = prevSepType;
                        }
                    }
                }
            }

            // Rule W5
            if (hasET && hasEN)
            {
                for (i = 0; i < this.runLength; ++i)
                {
                    if (this.runResolvedTypes[i] == BidiCharacterType.ET)
                    {
                        // Locate end of sequence
                        int seqStart = i;
                        int seqEnd = i;
                        while (seqEnd < this.runLength && this.runResolvedTypes[seqEnd] == BidiCharacterType.ET)
                        {
                            seqEnd++;
                        }

                        // Preceeded by, or followed by EN?
                        if ((seqStart == 0 ? sos : this.runResolvedTypes[seqStart - 1]) == BidiCharacterType.EN
                            || (seqEnd == this.runLength ? eos : this.runResolvedTypes[seqEnd]) == BidiCharacterType.EN)
                        {
                            // Change the entire range
                            for (int j = seqStart; i < seqEnd; ++i)
                            {
                                this.runResolvedTypes[i] = BidiCharacterType.EN;
                            }
                        }

                        // continue at end of sequence
                        i = seqEnd;
                    }
                }
            }

            // Rule W6
            if (hasES || hasET || hasCS)
            {
                for (i = 0; i < this.runLength; ++i)
                {
                    ref BidiCharacterType t = ref this.runResolvedTypes[i];
                    if (t == BidiCharacterType.ES || t == BidiCharacterType.ET || t == BidiCharacterType.CS)
                    {
                        t = BidiCharacterType.ON;
                    }
                }
            }

            // Rule W7.
            if (hasEN)
            {
                BidiCharacterType prevStrongType = sos;
                for (i = 0; i < this.runLength; ++i)
                {
                    ref BidiCharacterType rt = ref this.runResolvedTypes[i];
                    if (rt == BidiCharacterType.EN)
                    {
                        // If prev strong type was an L change this to L too
                        if (prevStrongType == BidiCharacterType.L)
                        {
                            this.runResolvedTypes[i] = BidiCharacterType.L;
                        }
                    }

                    // Remember previous strong type (NB: AL should already be changed to R)
                    if (rt == BidiCharacterType.L || rt == BidiCharacterType.R)
                    {
                        prevStrongType = rt;
                    }
                }
            }

            // Rule N0 - process bracket pairs
            if (this.hasBrackets)
            {
                int count;
                List<BracketPair>? pairedBrackets = this.LocatePairedBrackets();
                for (i = 0, count = pairedBrackets.Count; i < count; i++)
                {
                    BracketPair pb = pairedBrackets[i];
                    BidiCharacterType dir = this.InspectPairedBracket(pb);

                    // Case "d" - no strong types in the brackets, ignore
                    if (dir == BidiCharacterType.ON)
                    {
                        continue;
                    }

                    // Case "b" - strong type found that matches the embedding direction
                    if ((dir == BidiCharacterType.L || dir == BidiCharacterType.R) && dir == this.runDirection)
                    {
                        this.SetPairedBracketDirection(pb, dir);
                        continue;
                    }

                    // Case "c" - found opposite strong type found, look before to establish context
                    dir = this.InspectBeforePairedBracket(pb, sos);
                    if (dir == this.runDirection || dir == BidiCharacterType.ON)
                    {
                        dir = this.runDirection;
                    }

                    this.SetPairedBracketDirection(pb, dir);
                }
            }

            // Rules N1 and N2 - resolve neutral types
            for (i = 0; i < this.runLength; ++i)
            {
                BidiCharacterType t = this.runResolvedTypes[i];
                if (this.IsNeutralType(t))
                {
                    // Locate end of sequence
                    int seqStart = i;
                    int seqEnd = i;
                    while (seqEnd < this.runLength && this.IsNeutralType(this.runResolvedTypes[seqEnd]))
                    {
                        seqEnd++;
                    }

                    // Work out the preceding type
                    BidiCharacterType typeBefore;
                    if (seqStart == 0)
                    {
                        typeBefore = sos;
                    }
                    else
                    {
                        typeBefore = this.runResolvedTypes[seqStart - 1];
                        if (typeBefore == BidiCharacterType.AN || typeBefore == BidiCharacterType.EN)
                        {
                            typeBefore = BidiCharacterType.R;
                        }
                    }

                    // Work out the following type
                    BidiCharacterType typeAfter;
                    if (seqEnd == this.runLength)
                    {
                        typeAfter = eos;
                    }
                    else
                    {
                        typeAfter = this.runResolvedTypes[seqEnd];
                        if (typeAfter == BidiCharacterType.AN || typeAfter == BidiCharacterType.EN)
                        {
                            typeAfter = BidiCharacterType.R;
                        }
                    }

                    // Work out the final resolved type
                    BidiCharacterType resolvedType;
                    if (typeBefore == typeAfter)
                    {
                        // Rule N1
                        resolvedType = typeBefore;
                    }
                    else
                    {
                        // Rule N2
                        resolvedType = this.runDirection;
                    }

                    // Apply changes
                    for (int j = seqStart; j < seqEnd; j++)
                    {
                        this.runResolvedTypes[j] = resolvedType;
                    }

                    // continue after this run
                    i = seqEnd;
                }
            }

            // Rules I1 and I2 - resolve implicit types
            if ((this.runLevel & 0x01) == 0)
            {
                // Rule I1 - even
                for (i = 0; i < this.runLength; i++)
                {
                    BidiCharacterType t = this.runResolvedTypes[i];
                    ref sbyte l = ref this.runLevels[i];
                    if (t == BidiCharacterType.R)
                    {
                        l++;
                    }
                    else if (t == BidiCharacterType.AN || t == BidiCharacterType.EN)
                    {
                        l += 2;
                    }
                }
            }
            else
            {
                // Rule I2 - odd
                for (i = 0; i < this.runLength; i++)
                {
                    BidiCharacterType t = this.runResolvedTypes[i];
                    ref sbyte l = ref this.runLevels[i];
                    if (t != BidiCharacterType.R)
                    {
                        l++;
                    }
                }
            }
        }

        /// <summary>
        /// Locate all pair brackets in the current isolating run
        /// </summary>
        /// <returns>A sorted list of BracketPairs</returns>
        private List<BracketPair> LocatePairedBrackets()
        {
            // Clear work collections
            this.pendingOpeningBrackets.Clear();
            this.pairedBrackets.Clear();

            // Since List.Sort is expensive on memory if called often (it internally
            // allocates an ArraySorted object) and since we will rarely have many
            // items in this list (most paragraphs will only have a handful of bracket
            // pairs - if that), we use a simple linear lookup and insert most of the
            // time.  If there are more that `sortLimit` paired brackets we abort th
            // linear searching/inserting and using List.Sort at the end.
            const int sortLimit = 8;

            // Process all characters in the run, looking for paired brackets
            for (int ich = 0, length = this.runLength; ich < length; ich++)
            {
                // Ignore non-neutral characters
                if (this.runResolvedTypes[ich] != BidiCharacterType.ON)
                {
                    continue;
                }

                switch (this.runBidiPairedBracketTypes[ich])
                {
                    case BidiPairedBracketType.O:
                        if (this.pendingOpeningBrackets.Count == MaxPairedBracketDepth)
                        {
                            goto exit;
                        }

                        this.pendingOpeningBrackets.Insert(0, ich);
                        break;

                    case BidiPairedBracketType.C:
                        // see if there is a match
                        for (int i = 0; i < this.pendingOpeningBrackets.Count; i++)
                        {
                            if (this.runPairedBracketValues[ich] == this.runPairedBracketValues[this.pendingOpeningBrackets[i]])
                            {
                                // Add this paired bracket set
                                int opener = this.pendingOpeningBrackets[i];
                                if (this.pairedBrackets.Count < sortLimit)
                                {
                                    int ppi = 0;
                                    while (ppi < this.pairedBrackets.Count && this.pairedBrackets[ppi].OpeningIndex < opener)
                                    {
                                        ppi++;
                                    }

                                    this.pairedBrackets.Insert(ppi, new BracketPair(opener, ich));
                                }
                                else
                                {
                                    this.pairedBrackets.Add(new BracketPair(opener, ich));
                                }

                                // remove up to and including matched opener
                                this.pendingOpeningBrackets.RemoveRange(0, i + 1);
                                break;
                            }
                        }

                        break;
                }
            }

            exit:

            // Is a sort pending?
            if (this.pairedBrackets.Count > sortLimit)
            {
                this.pairedBrackets.Sort();
            }

            return this.pairedBrackets;
        }

        /// <summary>
        /// Inspect a paired bracket set and determine its strong direction
        /// </summary>
        /// <param name="pb">The paired bracket to be inpected</param>
        /// <returns>The direction of the bracket set content</returns>
        private BidiCharacterType InspectPairedBracket(in BracketPair pb)
        {
            BidiCharacterType dirEmbed = DirectionFromLevel(this.runLevel);
            BidiCharacterType dirOpposite = BidiCharacterType.ON;
            for (int ich = pb.OpeningIndex + 1; ich < pb.ClosingIndex; ich++)
            {
                BidiCharacterType dir = this.GetStrongTypeN0(this.runResolvedTypes[ich]);
                if (dir == BidiCharacterType.ON)
                {
                    continue;
                }

                if (dir == dirEmbed)
                {
                    return dir;
                }

                dirOpposite = dir;
            }

            return dirOpposite;
        }

        /// <summary>
        /// Look for a strong type before a paired bracket
        /// </summary>
        /// <param name="pb">The paired bracket set to be inspected</param>
        /// <param name="sos">The sos in case nothing found before the bracket</param>
        /// <returns>The strong direction before the brackets</returns>
        private BidiCharacterType InspectBeforePairedBracket(in BracketPair pb, BidiCharacterType sos)
        {
            for (int ich = pb.OpeningIndex - 1; ich >= 0; --ich)
            {
                BidiCharacterType dir = this.GetStrongTypeN0(this.runResolvedTypes[ich]);
                if (dir != BidiCharacterType.ON)
                {
                    return dir;
                }
            }

            return sos;
        }

        /// <summary>
        /// Sets the direction of a bracket pair, including setting the direction of
        /// NSM's inside the brackets and following.
        /// </summary>
        /// <param name="pb">The paired brackets</param>
        /// <param name="dir">The resolved direction for the bracket pair</param>
        private void SetPairedBracketDirection(in BracketPair pb, BidiCharacterType dir)
        {
            // Set the direction of the brackets
            this.runResolvedTypes[pb.OpeningIndex] = dir;
            this.runResolvedTypes[pb.ClosingIndex] = dir;

            // Set the directionality of NSM's inside the brackets
            for (int i = pb.OpeningIndex + 1; i < pb.ClosingIndex; i++)
            {
                if (this.runOriginalTypes[i] == BidiCharacterType.NSM)
                {
                    this.runOriginalTypes[i] = dir;
                }
                else
                {
                    break;
                }
            }

            // Set the directionality of NSM's following the brackets
            for (int i = pb.ClosingIndex + 1; i < this.runLength; i++)
            {
                if (this.runOriginalTypes[i] == BidiCharacterType.NSM)
                {
                    this.runResolvedTypes[i] = dir;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Resets whitespace levels. Implements rule L1
        /// </summary>
        private void ResetWhitespaceLevels()
        {
            for (int i = 0; i < this.resolvedLevels.Length; i++)
            {
                BidiCharacterType t = this.originalTypes[i];
                if (t == BidiCharacterType.B || t == BidiCharacterType.S)
                {
                    // Rule L1, clauses one and two.
                    this.resolvedLevels[i] = this.paragraphEmbeddingLevel;

                    // Rule L1, clause three.
                    for (int j = i - 1; j >= 0; --j)
                    {
                        if (IsWhitespace(this.originalTypes[j]))
                        {
                            // including format codes
                            this.resolvedLevels[j] = this.paragraphEmbeddingLevel;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Rule L1, clause four.
            for (int j = this.resolvedLevels.Length - 1; j >= 0; j--)
            {
                if (IsWhitespace(this.originalTypes[j]))
                { // including format codes
                    this.resolvedLevels[j] = this.paragraphEmbeddingLevel;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Assign levels to any characters that would be have been
        /// removed by rule X9.  The idea is to keep level runs together
        /// that would otherwise be broken by an interfering isolate/embedding
        /// control character.
        /// </summary>
        private void AssignLevelsToCodePointsRemovedByX9()
        {
            // Redundant?
            if (!this.hasIsolates && !this.hasEmbeddings)
            {
                return;
            }

            // No-op?
            if (this.workingTypes.Length == 0)
            {
                return;
            }

            // Fix up first character
            if (this.resolvedLevels[0] < 0)
            {
                this.resolvedLevels[0] = this.paragraphEmbeddingLevel;
            }

            if (IsRemovedByX9(this.originalTypes[0]))
            {
                this.workingTypes[0] = this.originalTypes[0];
            }

            for (int i = 1, length = this.workingTypes.Length; i < length; i++)
            {
                BidiCharacterType t = this.originalTypes[i];
                if (IsRemovedByX9(t))
                {
                    this.workingTypes[i] = t;
                    this.resolvedLevels[i] = this.resolvedLevels[i - 1];
                }
            }
        }

        /// <summary>
        /// Check if a directionality type represents whitepsace
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWhitespace(BidiCharacterType biditype)
        {
            switch (biditype)
            {
                case BidiCharacterType.LRE:
                case BidiCharacterType.RLE:
                case BidiCharacterType.LRO:
                case BidiCharacterType.RLO:
                case BidiCharacterType.PDF:
                case BidiCharacterType.LRI:
                case BidiCharacterType.RLI:
                case BidiCharacterType.FSI:
                case BidiCharacterType.PDI:
                case BidiCharacterType.BN:
                case BidiCharacterType.WS:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Convert a level to a direction where odd is RTL and
        /// even is LTR
        /// </summary>
        /// <param name="level">The level to convert</param>
        /// <returns>A directionality</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BidiCharacterType DirectionFromLevel(int level)
            => ((level & 0x1) == 0) ? BidiCharacterType.L : BidiCharacterType.R;

        /// <summary>
        /// Helper to check if a directionality is removed by rule X9
        /// </summary>
        /// <param name="biditype">The bidi type to check</param>
        /// <returns>True if rule X9 would remove this character; otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRemovedByX9(BidiCharacterType biditype)
        {
            switch (biditype)
            {
                case BidiCharacterType.LRE:
                case BidiCharacterType.RLE:
                case BidiCharacterType.LRO:
                case BidiCharacterType.RLO:
                case BidiCharacterType.PDF:
                case BidiCharacterType.BN:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if a a directionality is neutral for rules N1 and N2
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsNeutralType(BidiCharacterType dir)
        {
            switch (dir)
            {
                case BidiCharacterType.B:
                case BidiCharacterType.S:
                case BidiCharacterType.WS:
                case BidiCharacterType.ON:
                case BidiCharacterType.RLI:
                case BidiCharacterType.LRI:
                case BidiCharacterType.FSI:
                case BidiCharacterType.PDI:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Maps a direction to a strong type for rule N0
        /// </summary>
        /// <param name="dir">The direction to map</param>
        /// <returns>A strong direction - R, L or ON</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BidiCharacterType GetStrongTypeN0(BidiCharacterType dir)
        {
            switch (dir)
            {
                case BidiCharacterType.EN:
                case BidiCharacterType.AN:
                case BidiCharacterType.AL:
                case BidiCharacterType.R:
                    return BidiCharacterType.R;
                case BidiCharacterType.L:
                    return BidiCharacterType.L;
                default:
                    return BidiCharacterType.ON;
            }
        }

        /// <summary>
        /// Hold the start and end index of a pair of brackets
        /// </summary>
        private readonly struct BracketPair : IComparable<BracketPair>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BracketPair"/> struct.
            /// </summary>
            /// <param name="openingIndex">Index of the opening bracket</param>
            /// <param name="closingIndex">Index of the closing bracket</param>
            public BracketPair(int openingIndex, int closingIndex)
            {
                this.OpeningIndex = openingIndex;
                this.ClosingIndex = closingIndex;
            }

            /// <summary>
            /// Gets the index of the opening bracket
            /// </summary>
            public int OpeningIndex { get; }

            /// <summary>
            /// Gets the index of the closing bracket
            /// </summary>
            public int ClosingIndex { get; }

            public int CompareTo(BracketPair other)
                => this.OpeningIndex.CompareTo(other.OpeningIndex);
        }

        /// <summary>
        /// Status stack entry used while resolving explicit
        /// embedding levels
        /// </summary>
        private readonly struct Status
        {
            public Status(sbyte embeddingLevel, BidiCharacterType overrideStatus, bool isolateStatus)
            {
                this.EmbeddingLevel = embeddingLevel;
                this.OverrideStatus = overrideStatus;
                this.IsolateStatus = isolateStatus;
            }

            public sbyte EmbeddingLevel { get; }

            public BidiCharacterType OverrideStatus { get; }

            public bool IsolateStatus { get; }
        }

        /// <summary>
        /// Provides information about a level run - a continuous
        /// sequence of equal levels.
        /// </summary>
        private readonly struct LevelRun
        {
            public LevelRun(int start, int length, int level, BidiCharacterType sos, BidiCharacterType eos)
            {
                this.Start = start;
                this.Length = length;
                this.Level = level;
                this.Sos = sos;
                this.Eos = eos;
            }

            public int Start { get; }

            public int Length { get; }

            public int Level { get; }

            public BidiCharacterType Sos { get; }

            public BidiCharacterType Eos { get; }
        }
    }
}
