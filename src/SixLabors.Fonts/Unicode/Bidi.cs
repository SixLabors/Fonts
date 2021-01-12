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
    /// The Bidi algorithm uses a number of memory arrays for resolved 
    /// types, level information, bracket types, x9 removal maps and 
    /// more...
    /// 
    /// This implementation of the Bidi algorithm has been designed
    /// to reduce memory pressure on the GC by re-using the same 
    /// work buffers, so instances of this class should be re-used
    /// as much as possible.
    /// </remarks>
    internal sealed class Bidi
    {
        /// <summary>
        /// A per-thread instance that can be re-used as often
        /// as necessary.
        /// </summary>
        internal static ThreadLocal<Bidi> Instance = new ThreadLocal<Bidi>(() => new Bidi());

        /// <summary>
        /// Constructs a new instance of Bidi algorithm processor
        /// </summary>
        public Bidi()
        {
        }

        /// <summary>
        /// Get the resolved levels 
        /// </summary>
        public BufferSlice<sbyte> ResolvedLevels => _resolvedLevels;

        /// <summary>
        /// Get the resolved paragraph embedding level
        /// </summary>
        public int ResolvedParagraphEmbeddingLevel => _paragraphEmbeddingLevel;

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
            _isolatePairs.Clear();
            _workingTypesBuffer.Clear();
            _levelRuns.Clear();
            _resolvedLevelsBuffer.Clear();

            // Setup original types and working types
            _originalTypes = types;
            _workingTypes = _workingTypesBuffer.Add(types);

            // Capture paired bracket values and types
            _pairedBracketTypes = pairedBracketTypes;
            _pairedBracketValues = pairedBracketValues;

            // Store things we know
            _hasBrackets = hasBrackets ?? _pairedBracketTypes.Length == _originalTypes.Length;
            _hasEmbeddings = hasEmbeddings ?? true;
            _hasIsolates = hasIsolates ?? true;

            // Find all isolate pairs
            FindIsolatePairs();

            // Resolve the paragraph embedding level
            if (paragraphEmbeddingLevel == 2)
                _paragraphEmbeddingLevel = ResolveEmbeddingLevel(_originalTypes);
            else
                _paragraphEmbeddingLevel = paragraphEmbeddingLevel;

            // Create resolved levels buffer
            if (outLevels.HasValue)
            {
                if (outLevels.Value.Length != _originalTypes.Length)
                    throw new ArgumentException("Out levels must be the same length as the input data");
                _resolvedLevels = outLevels.Value;
            }
            else
            {
                _resolvedLevels = _resolvedLevelsBuffer.Add(_originalTypes.Length);
                _resolvedLevels.Fill(_paragraphEmbeddingLevel);
            }

            // Resolve explicit embedding levels (Rules X1-X8)
            ResolveExplicitEmbeddingLevels();

            // Build the rule X9 map
            BuildX9RemovalMap();

            // Process all isolated run sequences
            ProcessIsolatedRunSequences();

            // Reset whitespace levels
            ResetWhitespaceLevels();

            // Clean up
            AssignLevelsToCodePointsRemovedByX9();
        }


        /// <summary>
        /// The original BidiCharacterType types as provided by the caller
        /// </summary>
        BufferSlice<BidiCharacterType> _originalTypes;

        /// <summary>
        /// Paired bracket types as provided by caller
        /// </summary>
        BufferSlice<BidiPairedBracketType> _pairedBracketTypes;

        /// <summary>
        /// Paired bracket values as provided by caller
        /// </summary>
        BufferSlice<int> _pairedBracketValues;

        /// <summary>
        /// Try if the incoming data is known to contain brackets
        /// </summary>
        bool _hasBrackets;

        /// <summary>
        /// True if the incoming data is known to contain embedding runs
        /// </summary>
        bool _hasEmbeddings;

        /// <summary>
        /// True if the incomding data is known to contain isolating runs
        /// </summary>
        bool _hasIsolates;

        /// <summary>
        /// Two directional mapping of isolate start/end pairs
        /// </summary>
        /// <remarks>
        /// The forward mapping maps the start index to the end index.
        /// The reverse mapping maps the end index to the start index.
        /// </remarks>
        BidiDictionary<int, int> _isolatePairs = new BidiDictionary<int, int>();

        /// <summary>
        /// The working BidiCharacterType types
        /// </summary>
        BufferSlice<BidiCharacterType> _workingTypes;

        /// <summary>
        /// The buffer underlying _workingTypes
        /// </summary>
        ExpandableBuffer<BidiCharacterType> _workingTypesBuffer = new ExpandableBuffer<BidiCharacterType>();

        /// <summary>
        /// The resolved levels
        /// </summary>
        BufferSlice<sbyte> _resolvedLevels;

        /// <summary>
        /// The buffer underlying _resolvedLevels
        /// </summary>
        ExpandableBuffer<sbyte> _resolvedLevelsBuffer = new ExpandableBuffer<sbyte>();

        /// <summary>
        /// The resolve paragraph embedding level
        /// </summary>
        sbyte _paragraphEmbeddingLevel;

        /// <summary>
        /// Status stack entry used while resolving explicit
        /// embedding levels
        /// </summary>
        struct Status
        {
            public sbyte EmbeddingLevel;
            public BidiCharacterType OverrideStatus;
            public bool IsolateStatus;
        }

        /// <summary>
        /// The status stack used during resolution of explicit 
        /// embedding and isolating runs
        /// </summary>
        Stack<Status> _statusStack = new Stack<Status>();

        /// <summary>
        /// Mapping used to virtually remove characters for rule X9
        /// </summary>
        ExpandableBuffer<int> _X9Map = new ExpandableBuffer<int>();

        /// <summary>
        /// Re-usable list of level runs
        /// </summary>
        List<LevelRun> _levelRuns = new List<LevelRun>();

        /// <summary>
        /// Mapping for the current isolating sequence, built
        /// by joining level runs from the x9 map.
        /// </summary>
        ExpandableBuffer<int> _isolatedRunMapping = new ExpandableBuffer<int>();

        /// <summary>
        /// A stack of pending isolate openings used by FindIsolatePairs()
        /// </summary>
        Stack<int> _pendingIsolateOpenings = new Stack<int>();

        /// <summary>
        /// Build a list of matching isolates for a directionality slice 
        /// Implements BD9
        /// </summary>
        void FindIsolatePairs()
        {
            // Redundant?
            if (!_hasIsolates)
                return;

            // Lets double check this as we go and clear the flag
            // if there actually aren't any isolate pairs as this might
            // mean we can skip some later steps
            _hasIsolates = false;

            // BD9...
            _pendingIsolateOpenings.Clear();
            for (int i = 0; i < _originalTypes.Length; i++)
            {
                var t = _originalTypes[i];
                if (t == BidiCharacterType.LRI || t == BidiCharacterType.RLI || t == BidiCharacterType.FSI)
                {
                    _pendingIsolateOpenings.Push(i);
                    _hasIsolates = true;
                }
                else if (t == BidiCharacterType.PDI)
                {
                    if (_pendingIsolateOpenings.Count > 0)
                    {
                        _isolatePairs.Add(_pendingIsolateOpenings.Pop(), i);
                    }
                    _hasIsolates = true;
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
            if (!_hasIsolates && !_hasEmbeddings)
                return;

            // Work variables
            _statusStack.Clear();
            int overflowIsolateCount = 0;
            int overflowEmbeddingCount = 0;
            int validIsolateCount = 0;

            // Constants
            const int maxStackDepth = 125;

            // Rule X1 - setup initial state
            _statusStack.Clear();
            _statusStack.Push(new Status()
            {
                EmbeddingLevel = _paragraphEmbeddingLevel,
                OverrideStatus = BidiCharacterType.ON,         // Neutral
                IsolateStatus = false,
            });


            // Process all characters
            for (int i = 0; i < _originalTypes.Length; i++)
            {
                switch (_originalTypes[i])
                {
                    case BidiCharacterType.RLE:
                    {
                        // Rule X2
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 1) | 1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status()
                            {
                                EmbeddingLevel = newLevel,
                                OverrideStatus = BidiCharacterType.ON,
                                IsolateStatus = false,
                            });

                            _resolvedLevels[i] = newLevel;
                        }
                        else
                        {
                            if (overflowIsolateCount == 0)
                                overflowEmbeddingCount++;
                        }
                        break;
                    }

                    case BidiCharacterType.LRE:
                    {
                        // Rule X3
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 2) & ~1);
                        if (newLevel < maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status()
                            {
                                EmbeddingLevel = newLevel,
                                OverrideStatus = BidiCharacterType.ON,
                                IsolateStatus = false,
                            });

                            _resolvedLevels[i] = newLevel;
                        }
                        else
                        {
                            if (overflowIsolateCount == 0)
                                overflowEmbeddingCount++;
                        }
                        break;
                    }

                    case BidiCharacterType.RLO:
                    {
                        // Rule X4
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 1) | 1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status()
                            {
                                EmbeddingLevel = newLevel,
                                OverrideStatus = BidiCharacterType.R,
                                IsolateStatus = false,
                            });

                            _resolvedLevels[i] = newLevel;
                        }
                        else
                        {
                            if (overflowIsolateCount == 0)
                                overflowEmbeddingCount++;
                        }
                        break;
                    }

                    case BidiCharacterType.LRO:
                    {
                        // Rule X5
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 2) & ~1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status()
                            {
                                EmbeddingLevel = newLevel,
                                OverrideStatus = BidiCharacterType.L,
                                IsolateStatus = false,
                            });

                            _resolvedLevels[i] = newLevel;
                        }
                        else
                        {
                            if (overflowIsolateCount == 0)
                                overflowEmbeddingCount++;
                        }
                        break;
                    }

                    case BidiCharacterType.RLI:
                    case BidiCharacterType.LRI:
                    case BidiCharacterType.FSI:
                    {
                        // Rule X5a, X5b and X5c
                        var resolvedIsolate = _originalTypes[i];

                        if (resolvedIsolate == BidiCharacterType.FSI)
                        {
                            if (!_isolatePairs.TryGetValue(i, out var endOfIsolate))
                            {
                                endOfIsolate = _originalTypes.Length;
                            }

                            // Rule X5c
                            if (ResolveEmbeddingLevel(_originalTypes.Slice(i + 1, endOfIsolate - (i + 1))) == 1)
                                resolvedIsolate = BidiCharacterType.RLI;
                            else
                                resolvedIsolate = BidiCharacterType.LRI;
                        }

                        // Replace RLI's level with current embedding level
                        var tos = _statusStack.Peek();
                        _resolvedLevels[i] = tos.EmbeddingLevel;

                        // Apply override
                        if (tos.OverrideStatus != BidiCharacterType.ON)
                        {
                            _workingTypes[i] = tos.OverrideStatus;
                        }

                        // Work out new level
                        sbyte newLevel;
                        if (resolvedIsolate == BidiCharacterType.RLI)
                            newLevel = (sbyte)((tos.EmbeddingLevel + 1) | 1);
                        else
                            newLevel = (sbyte)((tos.EmbeddingLevel + 2) & ~1);

                        // Valid?
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            validIsolateCount++;
                            _statusStack.Push(new Status()
                            {
                                EmbeddingLevel = newLevel,
                                OverrideStatus = BidiCharacterType.ON,
                                IsolateStatus = true,
                            });
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
                        var tos = _statusStack.Peek();
                        _resolvedLevels[i] = tos.EmbeddingLevel;
                        if (tos.OverrideStatus != BidiCharacterType.ON)
                        {
                            _workingTypes[i] = tos.OverrideStatus;
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
                            while (!_statusStack.Peek().IsolateStatus)
                                _statusStack.Pop();
                            _statusStack.Pop();
                            validIsolateCount--;
                        }

                        var tos = _statusStack.Peek();
                        _resolvedLevels[i] = tos.EmbeddingLevel;
                        if (tos.OverrideStatus != BidiCharacterType.ON)
                        {
                            _workingTypes[i] = tos.OverrideStatus;
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
                            else
                            {
                                if (!_statusStack.Peek().IsolateStatus && _statusStack.Count >= 2)
                                {
                                    _statusStack.Pop();
                                }
                            }
                        }
                        break;
                    }

                    case BidiCharacterType.B:
                    {
                        // Rule X8
                        _resolvedLevels[i] = _paragraphEmbeddingLevel;
                        break;
                    }


                }
            }
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
            for (var i = 0; i < data.Length; ++i)
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
                        if (_isolatePairs.TryGetValue(data.Start + i, out i))
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
        /// Build a map to the original data positions that excludes all
        /// the types defined by rule X9
        /// </summary>
        void BuildX9RemovalMap()
        {
            // Reserve room for the x9 map
            _X9Map.Length = _originalTypes.Length;

            if (_hasEmbeddings || _hasIsolates)
            {
                // Build a map the removes all x9 characters
                var j = 0;
                for (int i = 0; i < _originalTypes.Length; i++)
                {
                    if (!IsRemovedByX9(_originalTypes[i]))
                    {
                        _X9Map[j++] = i;
                    }
                }

                // Set the final length
                _X9Map.Length = j;
            }
            else
            {
                for (int i = 0, count = _originalTypes.Length; i < count; i++)
                {
                    _X9Map[i] = i;
                }
            }

        }

        /// <summary>
        /// Find the original character index for an entry in the X9 map
        /// </summary>
        /// <param name="index">Index in the x9 removal map</param>
        /// <returns>Index to the original data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int mapX9(int index)
        {
            //return index < _X9Map.Length ? _X9Map[index] : _originalTypes.Length;
            return _X9Map[index];
        }

        /// <summary>
        /// Provides information about a level run - a continuous
        /// sequence of equal levels.
        /// </summary>
        struct LevelRun
        {
            public LevelRun(int start, int length, int level, BidiCharacterType sos, BidiCharacterType eos)
            {
                this.start = start;
                this.length = length;
                this.level = level;
                this.sos = sos;
                this.eos = eos;
            }

            public int start;
            public int length;
            public int level;
            public BidiCharacterType sos;
            public BidiCharacterType eos;
        }

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
        void AddLevelRun(int start, int length, int level)
        {
            // Get original indicies to first and last character in this run
            int firstCharIndex = mapX9(start);
            int lastCharIndex = mapX9(start + length - 1);

            // Work out sos
            int i = firstCharIndex - 1;
            while (i >= 0 && IsRemovedByX9(_originalTypes[i]))
                i--;
            var prevLevel = i < 0 ? _paragraphEmbeddingLevel : _resolvedLevels[i];
            var sos = DirectionFromLevel(Math.Max(prevLevel, level));

            // Work out eos
            var lastType = _workingTypes[lastCharIndex];
            int nextLevel;
            if (lastType == BidiCharacterType.LRI || lastType == BidiCharacterType.RLI || lastType == BidiCharacterType.FSI)
            {
                nextLevel = _paragraphEmbeddingLevel;
            }
            else
            {
                i = lastCharIndex + 1;
                while (i < _originalTypes.Length && IsRemovedByX9(_originalTypes[i]))
                    i++;
                nextLevel = i >= _originalTypes.Length ? _paragraphEmbeddingLevel : _resolvedLevels[i];
            }
            var eos = DirectionFromLevel(Math.Max(nextLevel, level));

            // Add the run            
            _levelRuns.Add(new LevelRun(start, length, level, sos, eos));
        }

        /// <summary>
        /// Find all runs of the same level, populating the _levelRuns
        /// collection
        /// </summary>
        void FindLevelRuns()
        {
            int currentLevel = -1;
            int runStart = 0;
            for (int i = 0; i < _X9Map.Length; ++i)
            {
                int level = _resolvedLevels[mapX9(i)];
                if (level != currentLevel)
                {
                    if (currentLevel != -1)
                    {
                        AddLevelRun(runStart, i - runStart, currentLevel);
                    }
                    currentLevel = level;
                    runStart = i;
                }
            }

            // Don't forget the final level run
            if (currentLevel != -1)
            {
                AddLevelRun(runStart, _X9Map.Length - runStart, currentLevel);
            }
        }

        /// <summary>
        /// Given a character index, find the level run that starts at that position
        /// </summary>
        /// <param name="index">The index into the original (unmapped) data</param>
        /// <returns>The index of the run that starts at that index</returns>
        int FindRunForIndex(int index)
        {
            for (int i = 0; i < _levelRuns.Count; i++)
            {
                // Passed index is for the original non-x9 filtered data, however
                // the level run ranges are for the x9 filtered data.  Convert before
                // comparing
                if (mapX9(_levelRuns[i].start) == index)
                    return i;
            }
            throw new InvalidOperationException("Internal error");
        }

        /// <summary>
        /// Determine and the process all isolated run sequences
        /// </summary>
        void ProcessIsolatedRunSequences()
        {
            // Find all runs with the same level
            FindLevelRuns();

            // Process them one at a time by first building
            // a mapping using slices from the x9 map for each
            // run section that needs to be joined together to
            // form an complete run.  That full run mapping
            // will be placed in _isolatedRunMapping and then
            // processed by ProcessIsolatedRunSequence().
            while (_levelRuns.Count > 0)
            {
                // Clear the mapping
                _isolatedRunMapping.Clear();

                // Combine mappings from this run and all runs that continue on from it
                var runIndex = 0;
                BidiCharacterType eos = _levelRuns[0].eos;
                BidiCharacterType sos = _levelRuns[0].sos;
                int level = _levelRuns[0].level;
                while (true)
                {
                    // Get the run
                    var r = _levelRuns[runIndex];

                    // The eos of the isolating run is the eos of the
                    // last level run that comprises it.
                    eos = r.eos;

                    // Remove this run as we've now processed it
                    _levelRuns.RemoveAt(runIndex);

                    // Add the x9 map indicies for the run range to the mapping
                    // for this isolated run
                    _isolatedRunMapping.Add(_X9Map.Slice(r.start, r.length));

                    // Get the last character and see if it's an isolating run with a matching
                    // PDI and concatenate that run to this one
                    int lastCharacterIndex = _isolatedRunMapping[_isolatedRunMapping.Length - 1];
                    var lastType = _originalTypes[lastCharacterIndex];
                    if ((lastType == BidiCharacterType.LRI || lastType == BidiCharacterType.RLI || lastType == BidiCharacterType.FSI) &&
                            _isolatePairs.TryGetValue(lastCharacterIndex, out var nextRunIndex))
                    {
                        // Find the continuing run index
                        runIndex = FindRunForIndex(nextRunIndex);
                    }
                    else
                    {
                        break;
                    }
                }

                // Process this isolated run
                ProcessIsolatedRunSequence(sos, eos, level);
            }
        }

        /// <summary>
        /// The level of the isolating run currently being processed
        /// </summary>
        int _runLevel;

        /// <summary>
        /// The direction of the isolating run currently being processed
        /// </summary>
        BidiCharacterType _runDirection;

        /// <summary>
        /// The length of the isolating run currently being processed
        /// </summary>
        int _runLength;

        /// <summary>
        /// A mapped slice of the resolved types for the isolating run currently
        /// being processed
        /// </summary>
        MappedBuffer<BidiCharacterType> _runResolvedTypes;

        /// <summary>
        /// A mapped slice of the original types for the isolating run currently
        /// being processed
        /// </summary>
        MappedBuffer<BidiCharacterType> _runOriginalTypes;

        /// <summary>
        /// A mapped slice of the run levels for the isolating run currently
        /// being processed
        /// </summary>
        MappedBuffer<sbyte> _runLevels;

        /// <summary>
        /// A mapped slice of the paired bracket types of the isolating 
        /// run currently being processed
        /// </summary>
        MappedBuffer<BidiPairedBracketType> _runBidiPairedBracketTypes;

        /// <summary>
        /// A mapped slice of the paired bracket values of the isolating 
        /// run currently being processed
        /// </summary>
        MappedBuffer<int> _runPairedBracketValues;

        /// <summary>
        /// Process a single isolated run sequence, where the character sequence
        /// mapping is currently held in _isolatedRunMapping.
        /// </summary>
        void ProcessIsolatedRunSequence(BidiCharacterType sos, BidiCharacterType eos, int runLevel)
        {
            // Create mappings onto the underlying data
            _runResolvedTypes = new MappedBuffer<BidiCharacterType>(_workingTypes, _isolatedRunMapping.AsSlice());
            _runOriginalTypes = new MappedBuffer<BidiCharacterType>(_originalTypes, _isolatedRunMapping.AsSlice());
            _runLevels = new MappedBuffer<sbyte>(_resolvedLevels, _isolatedRunMapping.AsSlice());
            if (_hasBrackets)
            {
                _runBidiPairedBracketTypes = new MappedBuffer<BidiPairedBracketType>(_pairedBracketTypes, _isolatedRunMapping.AsSlice());
                _runPairedBracketValues = new MappedBuffer<int>(_pairedBracketValues, _isolatedRunMapping.AsSlice());
            }
            _runLevel = runLevel;
            _runDirection = DirectionFromLevel(runLevel);
            _runLength = _runResolvedTypes.Length;

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
            var prevType = sos;
            for (i = 0; i < _runLength; i++)
            {
                var t = _runResolvedTypes[i];
                switch (t)
                {
                    case BidiCharacterType.NSM:
                        _runResolvedTypes[i] = prevType;
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
                for (i = 0; i < _runLength; i++)
                {
                    if (_runResolvedTypes[i] == BidiCharacterType.EN)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            var t = _runResolvedTypes[j];
                            if (t == BidiCharacterType.L || t == BidiCharacterType.R || t == BidiCharacterType.AL)
                            {
                                if (t == BidiCharacterType.AL)
                                {
                                    _runResolvedTypes[i] = BidiCharacterType.AN;
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
                for (i = 0; i < _runLength; i++)
                {
                    if (_runResolvedTypes[i] == BidiCharacterType.AL)
                    {
                        _runResolvedTypes[i] = BidiCharacterType.R;
                    }
                }
            }

            // Rule W4
            if ((hasES || hasCS) && (hasEN || hasAN))
            {
                for (i = 1; i < _runLength - 1; ++i)
                {
                    ref var rt = ref _runResolvedTypes[i];
                    if (rt == BidiCharacterType.ES)
                    {
                        var prevSepType = _runResolvedTypes[i - 1];
                        var succSepType = _runResolvedTypes[i + 1];

                        if (prevSepType == BidiCharacterType.EN && succSepType == BidiCharacterType.EN)
                        {
                            // ES between EN and EN
                            rt = BidiCharacterType.EN;
                        }
                    }
                    else if (rt == BidiCharacterType.CS)
                    {
                        var prevSepType = _runResolvedTypes[i - 1];
                        var succSepType = _runResolvedTypes[i + 1];

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
                for (i = 0; i < _runLength; ++i)
                {
                    if (_runResolvedTypes[i] == BidiCharacterType.ET)
                    {
                        // Locate end of sequence
                        int seqStart = i;
                        int seqEnd = i;
                        while (seqEnd < _runLength && _runResolvedTypes[seqEnd] == BidiCharacterType.ET)
                            seqEnd++;

                        // Preceeded by, or followed by EN?
                        if ((seqStart == 0 ? sos : _runResolvedTypes[seqStart - 1]) == BidiCharacterType.EN
                            || (seqEnd == _runLength ? eos : _runResolvedTypes[seqEnd]) == BidiCharacterType.EN)
                        {
                            // Change the entire range
                            for (int j = seqStart; i < seqEnd; ++i)
                            {
                                _runResolvedTypes[i] = BidiCharacterType.EN;
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
                for (i = 0; i < _runLength; ++i)
                {
                    ref var t = ref _runResolvedTypes[i];
                    if (t == BidiCharacterType.ES || t == BidiCharacterType.ET || t == BidiCharacterType.CS)
                    {
                        t = BidiCharacterType.ON;
                    }
                }
            }

            // Rule W7.
            if (hasEN)
            {
                var prevStrongType = sos;
                for (i = 0; i < _runLength; ++i)
                {
                    ref var rt = ref _runResolvedTypes[i];
                    if (rt == BidiCharacterType.EN)
                    {
                        // If prev strong type was an L change this to L too
                        if (prevStrongType == BidiCharacterType.L)
                        {
                            _runResolvedTypes[i] = BidiCharacterType.L;
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
            if (_hasBrackets)
            {
                int count;
                var pairedBrackets = LocatePairedBrackets();
                for (i = 0, count = pairedBrackets.Count; i < count; i++)
                {
                    var pb = pairedBrackets[i];
                    var dir = InspectPairedBracket(pb);

                    // Case "d" - no strong types in the brackets, ignore
                    if (dir == BidiCharacterType.ON)
                    {
                        continue;
                    }

                    // Case "b" - strong type found that matches the embedding direction
                    if ((dir == BidiCharacterType.L || dir == BidiCharacterType.R) && dir == _runDirection)
                    {
                        SetPairedBracketDirection(pb, dir);
                        continue;
                    }

                    // Case "c" - found opposite strong type found, look before to establish context
                    dir = InspectBeforePairedBracket(pb, sos);
                    if (dir == _runDirection || dir == BidiCharacterType.ON)
                    {
                        dir = _runDirection;
                    }
                    SetPairedBracketDirection(pb, dir);
                }
            }


            // Rules N1 and N2 - resolve neutral types
            for (i = 0; i < _runLength; ++i)
            {
                var t = _runResolvedTypes[i];
                if (IsNeutralType(t))
                {
                    // Locate end of sequence
                    int seqStart = i;
                    int seqEnd = i;
                    while (seqEnd < _runLength && IsNeutralType(_runResolvedTypes[seqEnd]))
                        seqEnd++;

                    // Work out the preceding type
                    BidiCharacterType typeBefore;
                    if (seqStart == 0)
                    {
                        typeBefore = sos;
                    }
                    else
                    {
                        typeBefore = _runResolvedTypes[seqStart - 1];
                        if (typeBefore == BidiCharacterType.AN || typeBefore == BidiCharacterType.EN)
                        {
                            typeBefore = BidiCharacterType.R;
                        }
                    }

                    // Work out the following type
                    BidiCharacterType typeAfter;
                    if (seqEnd == _runLength)
                    {
                        typeAfter = eos;
                    }
                    else
                    {
                        typeAfter = _runResolvedTypes[seqEnd];
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
                        resolvedType = _runDirection;
                    }

                    // Apply changes
                    for (int j = seqStart; j < seqEnd; j++)
                    {
                        _runResolvedTypes[j] = resolvedType;
                    }

                    // continue after this run
                    i = seqEnd;
                }
            }

            // Rules I1 and I2 - resolve implicit types
            if ((_runLevel & 0x01) == 0)
            {
                // Rule I1 - even
                for (i = 0; i < _runLength; i++)
                {
                    var t = _runResolvedTypes[i];
                    ref var l = ref _runLevels[i];
                    if (t == BidiCharacterType.R)
                        l++;
                    else if (t == BidiCharacterType.AN || t == BidiCharacterType.EN)
                        l += 2;
                }
            }
            else
            {
                // Rule I2 - odd
                for (i = 0; i < _runLength; i++)
                {
                    var t = _runResolvedTypes[i];
                    ref var l = ref _runLevels[i];
                    if (t != BidiCharacterType.R)
                        l++;
                }
            }
        }

        /// <summary>
        /// IComparer for BracketPairs
        /// </summary>
        class PairedBracketComparer : IComparer<BracketPair>
        {
            int IComparer<BracketPair>.Compare(BracketPair x, BracketPair y)
            {
                return x.OpeningIndex - y.OpeningIndex;
            }
        }

        /// <summary>
        /// An shared instance of the PairedBracket comparer
        /// </summary>
        static PairedBracketComparer _pairedBracketComparer = new PairedBracketComparer();

        /// <summary>
        /// Maximum pairing depth for paired brackets
        /// </summary>
        const int MaxPairedBracketDepth = 63;

        /// <summary>
        /// Re-useable list of pending opening brackets used by the 
        /// LocatePairedBrackets method
        /// </summary>
        List<int> _pendingOpeningBrackets = new List<int>();

        /// <summary>
        /// Resolved list of paired brackets
        /// </summary>
        List<BracketPair> _pairedBrackets = new List<BracketPair>();

        /// <summary>
        /// Locate all pair brackets in the current isolating run
        /// </summary>
        /// <returns>A sorted list of BracketPairs</returns>
        List<BracketPair> LocatePairedBrackets()
        {
            // Clear work collections
            _pendingOpeningBrackets.Clear();
            _pairedBrackets.Clear();

            // Since List.Sort is expensive on memory if called often (it internally
            // allocates an ArraySorted object) and since we will rarely have many
            // items in this list (most paragraphs will only have a handful of bracket
            // pairs - if that), we use a simple linear lookup and insert most of the 
            // time.  If there are more that `sortLimit` paired brackets we abort th
            // linear searching/inserting and using List.Sort at the end.
            const int sortLimit = 8;

            // Process all characters in the run, looking for paired brackets
            for (int ich = 0, length = _runLength; ich < length; ich++)
            {
                // Ignore non-neutral characters
                if (_runResolvedTypes[ich] != BidiCharacterType.ON)
                    continue;

                switch (_runBidiPairedBracketTypes[ich])
                {
                    case BidiPairedBracketType.O:
                        if (_pendingOpeningBrackets.Count == MaxPairedBracketDepth)
                            goto exit;

                        _pendingOpeningBrackets.Insert(0, ich);
                        break;

                    case BidiPairedBracketType.C:
                        // see if there is a match
                        for (int i = 0; i < _pendingOpeningBrackets.Count; i++)
                        {
                            if (_runPairedBracketValues[ich] == _runPairedBracketValues[_pendingOpeningBrackets[i]])
                            {
                                // Add this paired bracket set
                                var opener = _pendingOpeningBrackets[i];
                                if (_pairedBrackets.Count < sortLimit)
                                {
                                    int ppi = 0;
                                    while (ppi < _pairedBrackets.Count && _pairedBrackets[ppi].OpeningIndex < opener)
                                    {
                                        ppi++;
                                    }
                                    _pairedBrackets.Insert(ppi, new BracketPair(opener, ich));
                                }
                                else
                                {
                                    _pairedBrackets.Add(new BracketPair(opener, ich));
                                }

                                // remove up to and including matched opener
                                _pendingOpeningBrackets.RemoveRange(0, i + 1);
                                break;
                            }
                        }
                        break;
                }
            }

            exit:
            // Is a sort pending?
            if (_pairedBrackets.Count > sortLimit)
                _pairedBrackets.Sort(_pairedBracketComparer);

            return _pairedBrackets;
        }

        /// <summary>
        /// Inspect a paired bracket set and determine its strong direction
        /// </summary>
        /// <param name="pb">The paired bracket to be inpected</param>
        /// <returns>The direction of the bracket set content</returns>
        BidiCharacterType InspectPairedBracket(BracketPair pb)
        {
            var dirEmbed = DirectionFromLevel(_runLevel);
            var dirOpposite = BidiCharacterType.ON;
            for (int ich = pb.OpeningIndex + 1; ich < pb.ClosingIndex; ich++)
            {
                var dir = GetStrongTypeN0(_runResolvedTypes[ich]);
                if (dir == BidiCharacterType.ON)
                    continue;
                if (dir == dirEmbed)
                    return dir;
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
        BidiCharacterType InspectBeforePairedBracket(BracketPair pb, BidiCharacterType sos)
        {
            for (int ich = pb.OpeningIndex - 1; ich >= 0; --ich)
            {
                var dir = GetStrongTypeN0(_runResolvedTypes[ich]);
                if (dir != BidiCharacterType.ON)
                    return dir;
            }
            return sos;
        }

        /// <summary>
        /// Sets the direction of a bracket pair, including setting the direction of 
        /// NSM's inside the brackets and following.
        /// </summary>
        /// <param name="pb">The paired brackets</param>
        /// <param name="dir">The resolved direction for the bracket pair</param>
        void SetPairedBracketDirection(BracketPair pb, BidiCharacterType dir)
        {
            // Set the direction of the brackets
            _runResolvedTypes[pb.OpeningIndex] = dir;
            _runResolvedTypes[pb.ClosingIndex] = dir;

            // Set the directionality of NSM's inside the brackets
            for (int i = pb.OpeningIndex + 1; i < pb.ClosingIndex; i++)
            {
                if (_runOriginalTypes[i] == BidiCharacterType.NSM)
                    _runOriginalTypes[i] = dir;
                else
                    break;
            }

            // Set the directionality of NSM's following the brackets
            for (int i = pb.ClosingIndex + 1; i < _runLength; i++)
            {
                if (_runOriginalTypes[i] == BidiCharacterType.NSM)
                    _runResolvedTypes[i] = dir;
                else
                    break;
            }
        }

        /// <summary>
        /// Hold the start and end index of a pair of brackets
        /// </summary>
        struct BracketPair
        {
            /// <summary>
            /// Index of the opening bracket
            /// </summary>
            public int OpeningIndex;

            /// <summary>
            /// Index of the closing bracket
            /// </summary>
            public int ClosingIndex;

            /// <summary>
            /// Constructs a new paired bracket
            /// </summary>
            /// <param name="openingIndex">Index of the opening bracket</param>
            /// <param name="closingIndex">Index of the closing bracket</param>
            public BracketPair(int openingIndex, int closingIndex)
            {
                this.OpeningIndex = openingIndex;
                this.ClosingIndex = closingIndex;
            }
        }

        /// <summary>
        /// Resets whitespace levels. Implements rule L1
        /// </summary>
        void ResetWhitespaceLevels()
        {
            for (int i = 0; i < _resolvedLevels.Length; i++)
            {
                var t = _originalTypes[i];
                if (t == BidiCharacterType.B || t == BidiCharacterType.S)
                {
                    // Rule L1, clauses one and two.
                    _resolvedLevels[i] = _paragraphEmbeddingLevel;

                    // Rule L1, clause three.
                    for (int j = i - 1; j >= 0; --j)
                    {
                        if (IsWhitespace(_originalTypes[j]))
                        { // including format
                          // codes
                            _resolvedLevels[j] = _paragraphEmbeddingLevel;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Rule L1, clause four.
            for (int j = _resolvedLevels.Length - 1; j >= 0; j--)
            {
                if (IsWhitespace(_originalTypes[j]))
                { // including format codes
                    _resolvedLevels[j] = _paragraphEmbeddingLevel;
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
        void AssignLevelsToCodePointsRemovedByX9()
        {
            // Redundant?
            if (!_hasIsolates && !_hasEmbeddings)
                return;

            // No-op?
            if (_workingTypes.Length == 0)
                return;

            // Fix up first character
            if (_resolvedLevels[0] < 0)
                _resolvedLevels[0] = _paragraphEmbeddingLevel;
            if (IsRemovedByX9(_originalTypes[0]))
                _workingTypes[0] = _originalTypes[0];

            for (int i = 1, length = _workingTypes.Length; i < length; i++)
            {
                var t = _originalTypes[i];
                if (IsRemovedByX9(t))
                {
                    _workingTypes[i] = t;
                    _resolvedLevels[i] = _resolvedLevels[i - 1];
                }
            }
        }

        /// <summary>
        /// Check if a directionality type represents whitepsace
        /// </summary>
        /// <param name="biditype"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsWhitespace(BidiCharacterType biditype)
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
        static BidiCharacterType DirectionFromLevel(int level)
        {
            return ((level & 0x1) == 0) ? BidiCharacterType.L : BidiCharacterType.R;
        }

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
        /// <param name="dir"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsNeutralType(BidiCharacterType dir)
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
        BidiCharacterType GetStrongTypeN0(BidiCharacterType dir)
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
    }
}
