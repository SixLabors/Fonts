// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode;

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
internal ref struct BidiAlgorithm
{
    /// <summary>
    /// The original BidiCharacterType types as provided by the caller
    /// </summary>
    private ReadOnlyArraySlice<BidiCharacterType> originalTypes = default;

    /// <summary>
    /// Paired bracket types as provided by caller
    /// </summary>
    private ReadOnlyArraySlice<BidiPairedBracketType> pairedBracketTypes = default;

    /// <summary>
    /// Paired bracket values as provided by caller
    /// </summary>
    private ReadOnlyArraySlice<int> pairedBracketValues = default;

    /// <summary>
    /// Try if the incoming data is known to contain brackets
    /// </summary>
    private bool hasBrackets = default;

    /// <summary>
    /// True if the incoming data is known to contain embedding runs
    /// </summary>
    private bool hasEmbeddings = default;

    /// <summary>
    /// True if the incoming data is known to contain isolating runs
    /// </summary>
    private bool hasIsolates = default;

    /// <summary>
    /// Two directional mapping of isolate start/end pairs
    /// </summary>
    /// <remarks>
    /// The forward mapping maps the start index to the end index.
    /// The reverse mapping maps the end index to the start index.
    /// </remarks>
    private BidiDictionary<Index, Index> isolatePairs = default;

    /// <summary>
    /// The working BidiCharacterType types
    /// </summary>
    private ArraySlice<BidiCharacterType> workingTypes = default;

    /// <summary>
    /// The buffer underlying _workingTypes
    /// </summary>
    private ArrayBuilder<BidiCharacterType> workingTypesBuffer = default;

    /// <summary>
    /// The buffer underlying resolvedLevels
    /// </summary>
    private ArrayBuilder<sbyte> resolvedLevelsBuffer = default;

    /// <summary>
    /// The resolve paragraph embedding level
    /// </summary>
    private sbyte paragraphEmbeddingLevel = default;

    /// <summary>
    /// The status stack used during resolution of explicit
    /// embedding and isolating runs
    /// </summary>
    private Stack<Status> statusStack = default;

    /// <summary>
    /// Mapping used to virtually remove characters for rule X9
    /// </summary>
    private ArrayBuilder<int> x9Map = default;

    /// <summary>
    /// Re-usable list of level runs
    /// </summary>
    private ArrayBuilder<LevelRun> levelRuns = default;

    /// <summary>
    /// Mapping for the current isolating sequence, built
    /// by joining level runs from the x9 map.
    /// </summary>
    private ArrayBuilder<int> isolatedRunMapping = default;

    /// <summary>
    /// A stack of pending isolate openings used by FindIsolatePairs()
    /// </summary>
    private Stack<int> pendingIsolateOpenings = new();

    /// <summary>
    /// The level of the isolating run currently being processed
    /// </summary>
    private int runLevel = default;

    /// <summary>
    /// The direction of the isolating run currently being processed
    /// </summary>
    private BidiCharacterType runDirection = default;

    /// <summary>
    /// The length of the isolating run currently being processed
    /// </summary>
    private int runLength = default;

    /// <summary>
    /// A mapped slice of the resolved types for the isolating run currently
    /// being processed
    /// </summary>
    private MappedArraySlice<BidiCharacterType> runResolvedTypes = default;

    /// <summary>
    /// A mapped slice of the original types for the isolating run currently
    /// being processed
    /// </summary>
    private ReadonlyMappedArraySlice<BidiCharacterType> runOriginalTypes = default;

    /// <summary>
    /// A mapped slice of the run levels for the isolating run currently
    /// being processed
    /// </summary>
    private MappedArraySlice<sbyte> runLevels = default;

    /// <summary>
    /// A mapped slice of the paired bracket types of the isolating
    /// run currently being processed
    /// </summary>
    private ReadonlyMappedArraySlice<BidiPairedBracketType> runBidiPairedBracketTypes = default;

    /// <summary>
    /// A mapped slice of the paired bracket values of the isolating
    /// run currently being processed
    /// </summary>
    private ReadonlyMappedArraySlice<int> runPairedBracketValues = default;

    /// <summary>
    /// Maximum pairing depth for paired brackets
    /// </summary>
    private const int MaxPairedBracketDepth = 63;

    /// <summary>
    /// Reusable list of pending opening brackets used by the
    /// LocatePairedBrackets method
    /// </summary>
    private ArrayBuilder<int> pendingOpeningBrackets = default;

    /// <summary>
    /// Resolved list of paired brackets
    /// </summary>
    private ArrayBuilder<BracketPair> pairedBrackets = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="BidiAlgorithm"/> struct.
    /// </summary>
    public BidiAlgorithm()
    {
    }

    /// <summary>
    /// Gets the resolved levels.
    /// </summary>
    public ArraySlice<sbyte> ResolvedLevels { get; private set; } = default;

    /// <summary>
    /// Gets the resolved paragraph embedding level
    /// </summary>
    public readonly int ResolvedParagraphEmbeddingLevel => this.paragraphEmbeddingLevel;

    /// <summary>
    /// Process data from a BidiData instance
    /// </summary>
    /// <param name="data">The Bidi Unicode data.</param>
    public void Process(BidiData data)
        => this.Process(
            data.Types,
            data.PairedBracketTypes,
            data.PairedBracketValues,
            data.ParagraphEmbeddingLevel,
            data.HasBrackets,
            data.HasEmbeddings,
            data.HasIsolates,
            null);

    /// <summary>
    /// Processes Bidi Data
    /// </summary>
    public void Process(
        ReadOnlyArraySlice<BidiCharacterType> types,
        ReadOnlyArraySlice<BidiPairedBracketType> pairedBracketTypes,
        ReadOnlyArraySlice<int> pairedBracketValues,
        sbyte paragraphEmbeddingLevel,
        bool? hasBrackets,
        bool? hasEmbeddings,
        bool? hasIsolates,
        ArraySlice<sbyte>? outLevels)
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

            this.ResolvedLevels = outLevels.Value;
        }
        else
        {
            this.ResolvedLevels = this.resolvedLevelsBuffer.Add(this.originalTypes.Length);
            this.ResolvedLevels.Fill(this.paragraphEmbeddingLevel);
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

    public void Free()
    {
        this.isolatePairs.Free();
        this.workingTypesBuffer.Free();
        this.resolvedLevelsBuffer.Free();
        this.statusStack.Free();
        this.x9Map.Free();
        this.levelRuns.Free();
        this.isolatedRunMapping.Free();
        this.pendingIsolateOpenings.Free();
        this.pendingOpeningBrackets.Free();
        this.pairedBrackets.Free();
        this = default;
    }

    /// <summary>
    /// Resolve the paragraph embedding level if not explicitly passed
    /// by the caller. Also used by rule X5c for FSI isolating sequences.
    /// </summary>
    /// <param name="data">The data to be evaluated</param>
    /// <returns>The resolved embedding level</returns>
    private readonly sbyte ResolveEmbeddingLevel(ReadOnlyArraySlice<BidiCharacterType> data)
    {
        // P2
        for (int i = 0; i < data.Length; ++i)
        {
            switch (data[i])
            {
                case BidiCharacterType.LeftToRight:
                    // P3
                    return 0;

                case BidiCharacterType.ArabicLetter:
                case BidiCharacterType.RightToLeft:
                    // P3
                    return 1;

                case BidiCharacterType.FirstStrongIsolate:
                case BidiCharacterType.LeftToRightIsolate:
                case BidiCharacterType.RightToLeftIsolate:
                    // Skip isolate pairs
                    // (Because we're working with a slice, we need to adjust the indices
                    //  we're using for the isolatePairs map)
                    if (this.isolatePairs.TryGetValue(data.Start + i, out Index index))
                    {
                        i = index - data.Start;
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
            if (t is BidiCharacterType.LeftToRightIsolate
                or BidiCharacterType.RightToLeftIsolate
                or BidiCharacterType.FirstStrongIsolate)
            {
                this.pendingIsolateOpenings.Push(i);
                this.hasIsolates = true;
            }
            else if (t == BidiCharacterType.PopDirectionalIsolate)
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
        this.statusStack.Push(new Status(this.paragraphEmbeddingLevel, BidiCharacterType.OtherNeutral, false));

        // Process all characters
        for (int i = 0; i < this.originalTypes.Length; i++)
        {
            switch (this.originalTypes[i])
            {
                case BidiCharacterType.RightToLeftEmbedding:
                {
                    // Rule X2
                    sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 1) | 1);
                    if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                    {
                        this.statusStack.Push(new Status(newLevel, BidiCharacterType.OtherNeutral, false));
                        this.ResolvedLevels[i] = newLevel;
                    }
                    else if (overflowIsolateCount == 0)
                    {
                        overflowEmbeddingCount++;
                    }

                    break;
                }

                case BidiCharacterType.LeftToRightEmbedding:
                {
                    // Rule X3
                    sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 2) & ~1);
                    if (newLevel < maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                    {
                        this.statusStack.Push(new Status(newLevel, BidiCharacterType.OtherNeutral, false));
                        this.ResolvedLevels[i] = newLevel;
                    }
                    else if (overflowIsolateCount == 0)
                    {
                        overflowEmbeddingCount++;
                    }

                    break;
                }

                case BidiCharacterType.RightToLeftOverride:
                {
                    // Rule X4
                    sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 1) | 1);
                    if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                    {
                        this.statusStack.Push(new Status(newLevel, BidiCharacterType.RightToLeft, false));
                        this.ResolvedLevels[i] = newLevel;
                    }
                    else if (overflowIsolateCount == 0)
                    {
                        overflowEmbeddingCount++;
                    }

                    break;
                }

                case BidiCharacterType.LeftToRightOverride:
                {
                    // Rule X5
                    sbyte newLevel = (sbyte)((this.statusStack.Peek().EmbeddingLevel + 2) & ~1);
                    if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                    {
                        this.statusStack.Push(new Status(newLevel, BidiCharacterType.LeftToRight, false));
                        this.ResolvedLevels[i] = newLevel;
                    }
                    else if (overflowIsolateCount == 0)
                    {
                        overflowEmbeddingCount++;
                    }

                    break;
                }

                case BidiCharacterType.RightToLeftIsolate:
                case BidiCharacterType.LeftToRightIsolate:
                case BidiCharacterType.FirstStrongIsolate:
                {
                    // Rule X5a, X5b and X5c
                    BidiCharacterType resolvedIsolate = this.originalTypes[i];

                    if (resolvedIsolate == BidiCharacterType.FirstStrongIsolate)
                    {
                        if (!this.isolatePairs.TryGetValue(i, out Index endOfIsolate))
                        {
                            endOfIsolate = this.originalTypes.Length;
                        }

                        // Rule X5c
                        if (this.ResolveEmbeddingLevel(this.originalTypes[(i + 1)..(int)endOfIsolate]) == 1)
                        {
                            resolvedIsolate = BidiCharacterType.RightToLeftIsolate;
                        }
                        else
                        {
                            resolvedIsolate = BidiCharacterType.LeftToRightIsolate;
                        }
                    }

                    // Replace RLI's level with current embedding level
                    Status tos = this.statusStack.Peek();
                    this.ResolvedLevels[i] = tos.EmbeddingLevel;

                    // Apply override
                    if (tos.OverrideStatus != BidiCharacterType.OtherNeutral)
                    {
                        this.workingTypes[i] = tos.OverrideStatus;
                    }

                    // Work out new level
                    sbyte newLevel;
                    if (resolvedIsolate == BidiCharacterType.RightToLeftIsolate)
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
                        this.statusStack.Push(new Status(newLevel, BidiCharacterType.OtherNeutral, true));
                    }
                    else
                    {
                        overflowIsolateCount++;
                    }

                    break;
                }

                case BidiCharacterType.BoundaryNeutral:
                {
                    // Mentioned in rule X6 - "for all types besides ..., BN, ..."
                    // no-op
                    break;
                }

                default:
                {
                    // Rule X6
                    Status tos = this.statusStack.Peek();
                    this.ResolvedLevels[i] = tos.EmbeddingLevel;
                    if (tos.OverrideStatus != BidiCharacterType.OtherNeutral)
                    {
                        this.workingTypes[i] = tos.OverrideStatus;
                    }

                    break;
                }

                case BidiCharacterType.PopDirectionalIsolate:
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
                    this.ResolvedLevels[i] = tos.EmbeddingLevel;
                    if (tos.OverrideStatus != BidiCharacterType.OtherNeutral)
                    {
                        this.workingTypes[i] = tos.OverrideStatus;
                    }

                    break;
                }

                case BidiCharacterType.PopDirectionalFormat:
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

                case BidiCharacterType.ParagraphSeparator:
                {
                    // Rule X8
                    this.ResolvedLevels[i] = this.paragraphEmbeddingLevel;
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
    private readonly int MapX9(int index) => this.x9Map[index];

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
    private readonly void AddLevelRun(int start, int length, int level)
    {
        // Get original indices to first and last character in this run
        int firstCharIndex = this.MapX9(start);
        int lastCharIndex = this.MapX9(start + length - 1);

        // Work out sos
        int i = firstCharIndex - 1;
        while (i >= 0 && IsRemovedByX9(this.originalTypes[i]))
        {
            i--;
        }

        sbyte prevLevel = i < 0 ? this.paragraphEmbeddingLevel : this.ResolvedLevels[i];
        BidiCharacterType sos = DirectionFromLevel(Math.Max(prevLevel, level));

        // Work out eos
        BidiCharacterType lastType = this.workingTypes[lastCharIndex];
        int nextLevel;
        if (lastType is BidiCharacterType.LeftToRightIsolate
            or BidiCharacterType.RightToLeftIsolate
            or BidiCharacterType.FirstStrongIsolate)
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

            nextLevel = i >= this.originalTypes.Length ? this.paragraphEmbeddingLevel : this.ResolvedLevels[i];
        }

        BidiCharacterType eos = DirectionFromLevel(Math.Max(nextLevel, level));

        // Add the run
        this.levelRuns.Add(new LevelRun(start, length, level, sos, eos));
    }

    /// <summary>
    /// Find all runs of the same level, populating the _levelRuns
    /// collection
    /// </summary>
    private readonly void FindLevelRuns()
    {
        int currentLevel = -1;
        int runStart = 0;
        for (int i = 0; i < this.x9Map.Length; ++i)
        {
            int level = this.ResolvedLevels[this.MapX9(i)];
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
    private readonly int FindRunForIndex(int index)
    {
        for (int i = 0; i < this.levelRuns.Length; i++)
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
        while (this.levelRuns.Length > 0)
        {
            // Clear the mapping
            this.isolatedRunMapping.Clear();

            // Combine mappings from this run and all runs that continue on from it
            int runIndex = 0;
            BidiCharacterType eos;
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

                // Add the x9 map indices for the run range to the mapping
                // for this isolated run
                this.isolatedRunMapping.Add(this.x9Map.AsSlice(r.Start, r.Length));

                // Get the last character and see if it's an isolating run with a matching
                // PDI and concatenate that run to this one
                int lastCharacterIndex = this.isolatedRunMapping[this.isolatedRunMapping.Length - 1];
                BidiCharacterType lastType = this.originalTypes[lastCharacterIndex];
                if ((lastType == BidiCharacterType.LeftToRightIsolate || lastType == BidiCharacterType.RightToLeftIsolate || lastType == BidiCharacterType.FirstStrongIsolate) &&
                        this.isolatePairs.TryGetValue(lastCharacterIndex, out Index nextRunIndex))
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
        this.runResolvedTypes = new MappedArraySlice<BidiCharacterType>(this.workingTypes, this.isolatedRunMapping.AsSlice());
        this.runOriginalTypes = new ReadonlyMappedArraySlice<BidiCharacterType>(this.originalTypes, this.isolatedRunMapping.AsSlice());
        this.runLevels = new MappedArraySlice<sbyte>(this.ResolvedLevels, this.isolatedRunMapping.AsSlice());
        if (this.hasBrackets)
        {
            this.runBidiPairedBracketTypes = new ReadonlyMappedArraySlice<BidiPairedBracketType>(this.pairedBracketTypes, this.isolatedRunMapping.AsSlice());
            this.runPairedBracketValues = new ReadonlyMappedArraySlice<int>(this.pairedBracketValues, this.isolatedRunMapping.AsSlice());
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
                case BidiCharacterType.NonspacingMark:
                    this.runResolvedTypes[i] = prevType;
                    break;

                case BidiCharacterType.LeftToRightIsolate:
                case BidiCharacterType.RightToLeftIsolate:
                case BidiCharacterType.FirstStrongIsolate:
                case BidiCharacterType.PopDirectionalIsolate:
                    prevType = BidiCharacterType.OtherNeutral;
                    break;

                case BidiCharacterType.EuropeanNumber:
                    hasEN = true;
                    prevType = t;
                    break;

                case BidiCharacterType.ArabicLetter:
                    hasAL = true;
                    prevType = t;
                    break;

                case BidiCharacterType.EuropeanSeparator:
                    hasES = true;
                    prevType = t;
                    break;

                case BidiCharacterType.CommonSeparator:
                    hasCS = true;
                    prevType = t;
                    break;

                case BidiCharacterType.ArabicNumber:
                    hasAN = true;
                    prevType = t;
                    break;

                case BidiCharacterType.EuropeanTerminator:
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
                if (this.runResolvedTypes[i] == BidiCharacterType.EuropeanNumber)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        BidiCharacterType t = this.runResolvedTypes[j];
                        if (t is BidiCharacterType.LeftToRight
                            or BidiCharacterType.RightToLeft
                            or BidiCharacterType.ArabicLetter)
                        {
                            if (t == BidiCharacterType.ArabicLetter)
                            {
                                this.runResolvedTypes[i] = BidiCharacterType.ArabicNumber;
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
                if (this.runResolvedTypes[i] == BidiCharacterType.ArabicLetter)
                {
                    this.runResolvedTypes[i] = BidiCharacterType.RightToLeft;
                }
            }
        }

        // Rule W4
        if ((hasES || hasCS) && (hasEN || hasAN))
        {
            for (i = 1; i < this.runLength - 1; ++i)
            {
                ref BidiCharacterType rt = ref this.runResolvedTypes[i];
                if (rt == BidiCharacterType.EuropeanSeparator)
                {
                    BidiCharacterType prevSepType = this.runResolvedTypes[i - 1];
                    BidiCharacterType succSepType = this.runResolvedTypes[i + 1];

                    if (prevSepType == BidiCharacterType.EuropeanNumber && succSepType == BidiCharacterType.EuropeanNumber)
                    {
                        // ES between EN and EN
                        rt = BidiCharacterType.EuropeanNumber;
                    }
                }
                else if (rt == BidiCharacterType.CommonSeparator)
                {
                    BidiCharacterType prevSepType = this.runResolvedTypes[i - 1];
                    BidiCharacterType succSepType = this.runResolvedTypes[i + 1];

                    if ((prevSepType == BidiCharacterType.ArabicNumber && succSepType == BidiCharacterType.ArabicNumber) ||
                         (prevSepType == BidiCharacterType.EuropeanNumber && succSepType == BidiCharacterType.EuropeanNumber))
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
                if (this.runResolvedTypes[i] == BidiCharacterType.EuropeanTerminator)
                {
                    // Locate end of sequence
                    int seqStart = i;
                    int seqEnd = i;
                    while (seqEnd < this.runLength && this.runResolvedTypes[seqEnd] == BidiCharacterType.EuropeanTerminator)
                    {
                        seqEnd++;
                    }

                    // Preceded by, or followed by EN?
                    if ((seqStart == 0 ? sos : this.runResolvedTypes[seqStart - 1]) == BidiCharacterType.EuropeanNumber
                        || (seqEnd == this.runLength ? eos : this.runResolvedTypes[seqEnd]) == BidiCharacterType.EuropeanNumber)
                    {
                        // Change the entire range
                        for (int j = seqStart; i < seqEnd; ++i)
                        {
                            this.runResolvedTypes[i] = BidiCharacterType.EuropeanNumber;
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
                if (t is BidiCharacterType.EuropeanSeparator
                    or BidiCharacterType.EuropeanTerminator
                    or BidiCharacterType.CommonSeparator)
                {
                    t = BidiCharacterType.OtherNeutral;
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
                if (rt == BidiCharacterType.EuropeanNumber)
                {
                    // If prev strong type was an L change this to L too
                    if (prevStrongType == BidiCharacterType.LeftToRight)
                    {
                        this.runResolvedTypes[i] = BidiCharacterType.LeftToRight;
                    }
                }

                // Remember previous strong type (NB: AL should already be changed to R)
                if (rt is BidiCharacterType.LeftToRight or BidiCharacterType.RightToLeft)
                {
                    prevStrongType = rt;
                }
            }
        }

        // Rule N0 - process bracket pairs
        if (this.hasBrackets)
        {
            int count;
            ArrayBuilder<BracketPair> pairedBrackets = this.LocatePairedBrackets();
            for (i = 0, count = pairedBrackets.Length; i < count; i++)
            {
                BracketPair pb = pairedBrackets[i];
                BidiCharacterType dir = this.InspectPairedBracket(pb);

                // Case "d" - no strong types in the brackets, ignore
                if (dir == BidiCharacterType.OtherNeutral)
                {
                    continue;
                }

                // Case "b" - strong type found that matches the embedding direction
                if ((dir == BidiCharacterType.LeftToRight || dir == BidiCharacterType.RightToLeft) && dir == this.runDirection)
                {
                    this.SetPairedBracketDirection(pb, dir);
                    continue;
                }

                // Case "c" - found opposite strong type found, look before to establish context
                dir = this.InspectBeforePairedBracket(pb, sos);
                if (dir == this.runDirection || dir == BidiCharacterType.OtherNeutral)
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
            if (IsNeutralType(t))
            {
                // Locate end of sequence
                int seqStart = i;
                int seqEnd = i;
                while (seqEnd < this.runLength && IsNeutralType(this.runResolvedTypes[seqEnd]))
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
                    if (typeBefore is BidiCharacterType.ArabicNumber or BidiCharacterType.EuropeanNumber)
                    {
                        typeBefore = BidiCharacterType.RightToLeft;
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
                    if (typeAfter is BidiCharacterType.ArabicNumber or BidiCharacterType.EuropeanNumber)
                    {
                        typeAfter = BidiCharacterType.RightToLeft;
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
                if (t == BidiCharacterType.RightToLeft)
                {
                    l++;
                }
                else if (t is BidiCharacterType.ArabicNumber or BidiCharacterType.EuropeanNumber)
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
                if (t != BidiCharacterType.RightToLeft)
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
    private readonly ArrayBuilder<BracketPair> LocatePairedBrackets()
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
            if (this.runResolvedTypes[ich] != BidiCharacterType.OtherNeutral)
            {
                continue;
            }

            switch (this.runBidiPairedBracketTypes[ich])
            {
                case BidiPairedBracketType.Open:
                    if (this.pendingOpeningBrackets.Length == MaxPairedBracketDepth)
                    {
                        goto exit;
                    }

                    this.pendingOpeningBrackets.Insert(0, ich);
                    break;

                case BidiPairedBracketType.Close:
                    // see if there is a match
                    for (int i = 0; i < this.pendingOpeningBrackets.Length; i++)
                    {
                        if (this.runPairedBracketValues[ich] == this.runPairedBracketValues[this.pendingOpeningBrackets[i]])
                        {
                            // Add this paired bracket set
                            int opener = this.pendingOpeningBrackets[i];
                            if (this.pairedBrackets.Length < sortLimit)
                            {
                                int ppi = 0;
                                while (ppi < this.pairedBrackets.Length && this.pairedBrackets[ppi].OpeningIndex < opener)
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
        if (this.pairedBrackets.Length > sortLimit)
        {
            this.pairedBrackets.Sort();
        }

        return this.pairedBrackets;
    }

    /// <summary>
    /// Inspect a paired bracket set and determine its strong direction
    /// </summary>
    /// <param name="pb">The paired bracket to be inspected</param>
    /// <returns>The direction of the bracket set content</returns>
    private readonly BidiCharacterType InspectPairedBracket(in BracketPair pb)
    {
        BidiCharacterType dirEmbed = DirectionFromLevel(this.runLevel);
        BidiCharacterType dirOpposite = BidiCharacterType.OtherNeutral;
        for (int ich = pb.OpeningIndex + 1; ich < pb.ClosingIndex; ich++)
        {
            BidiCharacterType dir = GetStrongTypeN0(this.runResolvedTypes[ich]);
            if (dir == BidiCharacterType.OtherNeutral)
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
    private readonly BidiCharacterType InspectBeforePairedBracket(in BracketPair pb, BidiCharacterType sos)
    {
        for (int ich = pb.OpeningIndex - 1; ich >= 0; --ich)
        {
            BidiCharacterType dir = GetStrongTypeN0(this.runResolvedTypes[ich]);
            if (dir != BidiCharacterType.OtherNeutral)
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
        // BN  characters (such as ZWJ or ZWSP) that appear between the base bracket character
        // and the nonspacing mark should be ignored.
        for (int i = pb.OpeningIndex + 1; i < pb.ClosingIndex; i++)
        {
            if (this.runOriginalTypes[i] == BidiCharacterType.NonspacingMark)
            {
                this.runResolvedTypes[i] = dir;
            }
            else if (this.runOriginalTypes[i] != BidiCharacterType.BoundaryNeutral)
            {
                break;
            }
        }

        // Set the directionality of NSM's following the brackets
        for (int i = pb.ClosingIndex + 1; i < this.runLength; i++)
        {
            if (this.runOriginalTypes[i] == BidiCharacterType.NonspacingMark)
            {
                this.runResolvedTypes[i] = dir;
            }
            else if (this.runOriginalTypes[i] != BidiCharacterType.BoundaryNeutral)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Resets whitespace levels. Implements rule L1
    /// </summary>
    private readonly void ResetWhitespaceLevels()
    {
        for (int i = 0; i < this.ResolvedLevels.Length; i++)
        {
            BidiCharacterType t = this.originalTypes[i];
            if (t is BidiCharacterType.ParagraphSeparator or BidiCharacterType.SegmentSeparator)
            {
                // Rule L1, clauses one and two.
                this.ResolvedLevels[i] = this.paragraphEmbeddingLevel;

                // Rule L1, clause three.
                for (int j = i - 1; j >= 0; --j)
                {
                    if (IsWhitespace(this.originalTypes[j]))
                    {
                        // including format codes
                        this.ResolvedLevels[j] = this.paragraphEmbeddingLevel;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // Rule L1, clause four.
        for (int j = this.ResolvedLevels.Length - 1; j >= 0; j--)
        {
            if (IsWhitespace(this.originalTypes[j]))
            { // including format codes
                this.ResolvedLevels[j] = this.paragraphEmbeddingLevel;
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
        if (this.ResolvedLevels[0] < 0)
        {
            this.ResolvedLevels[0] = this.paragraphEmbeddingLevel;
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
                this.ResolvedLevels[i] = this.ResolvedLevels[i - 1];
            }
        }
    }

    /// <summary>
    /// Check if a directionality type represents whitespace
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespace(BidiCharacterType biditype)
        => biditype switch
        {
            BidiCharacterType.LeftToRightEmbedding
            or BidiCharacterType.RightToLeftEmbedding
            or BidiCharacterType.LeftToRightOverride
            or BidiCharacterType.RightToLeftOverride
            or BidiCharacterType.PopDirectionalFormat
            or BidiCharacterType.LeftToRightIsolate
            or BidiCharacterType.RightToLeftIsolate
            or BidiCharacterType.FirstStrongIsolate
            or BidiCharacterType.PopDirectionalIsolate
            or BidiCharacterType.BoundaryNeutral
            or BidiCharacterType.Whitespace => true,
            _ => false,
        };

    /// <summary>
    /// Convert a level to a direction where odd is RTL and
    /// even is LTR
    /// </summary>
    /// <param name="level">The level to convert</param>
    /// <returns>A directionality</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BidiCharacterType DirectionFromLevel(int level)
        => ((level & 0x1) == 0) ? BidiCharacterType.LeftToRight : BidiCharacterType.RightToLeft;

    /// <summary>
    /// Helper to check if a directionality is removed by rule X9
    /// </summary>
    /// <param name="biditype">The bidi type to check</param>
    /// <returns>True if rule X9 would remove this character; otherwise false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRemovedByX9(BidiCharacterType biditype)
        => biditype switch
        {
            BidiCharacterType.LeftToRightEmbedding
            or BidiCharacterType.RightToLeftEmbedding
            or BidiCharacterType.LeftToRightOverride
            or BidiCharacterType.RightToLeftOverride
            or BidiCharacterType.PopDirectionalFormat
            or BidiCharacterType.BoundaryNeutral => true,
            _ => false,
        };

    /// <summary>
    /// Check if a a directionality is neutral for rules N1 and N2
    /// </summary>
    /// <param name="dir">The direction.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNeutralType(BidiCharacterType dir)
        => dir switch
        {
            BidiCharacterType.ParagraphSeparator
            or BidiCharacterType.SegmentSeparator
            or BidiCharacterType.Whitespace
            or BidiCharacterType.OtherNeutral
            or BidiCharacterType.RightToLeftIsolate
            or BidiCharacterType.LeftToRightIsolate
            or BidiCharacterType.FirstStrongIsolate
            or BidiCharacterType.PopDirectionalIsolate => true,
            _ => false,
        };

    /// <summary>
    /// Maps a direction to a strong type for rule N0
    /// </summary>
    /// <param name="dir">The direction to map</param>
    /// <returns>A strong direction - R, L or ON</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BidiCharacterType GetStrongTypeN0(BidiCharacterType dir)
        => dir switch
        {
            BidiCharacterType.EuropeanNumber
            or BidiCharacterType.ArabicNumber
            or BidiCharacterType.ArabicLetter
            or BidiCharacterType.RightToLeft => BidiCharacterType.RightToLeft,
            BidiCharacterType.LeftToRight => BidiCharacterType.LeftToRight,
            _ => BidiCharacterType.OtherNeutral,
        };

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

    public ref struct Stack<T>
    where T : unmanaged
    {
        private ArrayBuilder<T> items = default;

        public Stack()
        {
        }

        public readonly int Count => this.items.Length;

        public void Push(T item) => this.items.Add(item);

        public T Pop()
        {
            T result = this.items[this.items.Length - 1];
            this.items.Length--;
            return result;
        }

        public readonly T Peek() => this.items[this.items.Length - 1];

        public void Clear() => this.items.Clear();

        public void Free()
        {
            this.items.Free();
            this.items = default;
        }
    }

    private struct Index : IEqualityComparer<Index>
    {
        public int Value;

        public static implicit operator Index(int value) => new() { Value = value };

        public static implicit operator int(Index value) => value.Value;

        public readonly bool Equals(Index x, Index y) => x.Value == y.Value;

        public readonly int GetHashCode([DisallowNull] Index obj) => obj.Value;
    }
}
