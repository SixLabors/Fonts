// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SixLabors.Fonts.Unicode.Dfa
{
    internal class StateMachine
    {
        private const int InitialState = 1;
        private const int FailState = 0;

        private readonly int[][] stateTable;
        private readonly bool[] accepting;
        private readonly ICollection<string>[] tags;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine"/> class.
        /// </summary>
        /// <param name="stateTable">The state table.</param>
        /// <param name="accepting">The accepting states.</param>
        /// <param name="tags">The tags.</param>
        public StateMachine(int[][] stateTable, bool[] accepting, ICollection<string>[] tags)
        {
            this.stateTable = stateTable;
            this.accepting = accepting;
            this.tags = tags;
        }

        /// <summary>
        /// Returns an iterable object that yields pattern matches over the input sequence.
        /// </summary>
        /// <param name="input">The input sequence.</param>
        /// <returns>The <see cref="IEnumerable{StateMatch}"/>.</returns>
        public IEnumerable<StateMatch> Match(int[] input)
        {
            int state = InitialState;
            int? startRun = null;
            int? lastAccepting = null;
            int? lastState = null;

            for (int i = 0; i < input.Length; i++)
            {
                int c = input[i];

                lastState = state;
                state = this.stateTable[state][c];

                if (state == FailState)
                {
                    // yield the last match if any.
                    if (startRun != null && lastAccepting != null && lastAccepting >= startRun)
                    {
                        yield return new StateMatch()
                        {
                            StartIndex = startRun.Value,
                            EndIndex = lastAccepting.Value,
                            Tags = this.tags[lastState.Value]
                        };
                    }

                    // reset the state as if we started over from the initial state
                    state = this.stateTable[InitialState][c];
                    startRun = null;
                }

                // start a run if not in the failure state
                if (state != FailState && startRun == null)
                {
                    startRun = i;
                }

                // if accepting, mark the potential match end
                if (this.accepting[state])
                {
                    lastAccepting = i;
                }

                // reset the state to the initial state if we get into the failure state
                if (state == FailState)
                {
                    state = InitialState;
                }
            }

            // yield the last match if any.
            if (startRun != null && lastAccepting != null && lastAccepting >= startRun)
            {
                yield return new StateMatch()
                {
                    StartIndex = startRun.Value,
                    EndIndex = lastAccepting.Value,
                    Tags = lastState != null ? this.tags[lastState.Value] : Array.Empty<string>()
                };
            }
        }

        /// <summary>
        /// For each match over the input sequence, action functions matching
        /// the tag definitions in the input pattern are called with the startIndex,
        /// length, and the sequence to be sliced.
        /// </summary>
        /// <param name="input">The input sequence.</param>
        /// <param name="actions">The collection of actions.</param>
        public void Apply(int[] input, Dictionary<string, Action<int, int, int[]>> actions)
        {
            foreach (StateMatch match in this.Match(input))
            {
                foreach (string tag in match.Tags)
                {
                    if (actions.TryGetValue(tag, out Action<int, int, int[]>? action))
                    {
                        action(match.StartIndex, match.EndIndex + 1 - match.StartIndex, input);
                    }
                }
            }
        }
    }

    internal class StateMatch : IEquatable<StateMatch?>
    {
        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public ICollection<string> Tags { get; set; } = Array.Empty<string>();

        public override bool Equals(object? obj) => this.Equals(obj as StateMatch);

        public bool Equals(StateMatch? other)
            => other is not null
            && this.StartIndex == other.StartIndex
            && this.EndIndex == other.EndIndex
            && this.Tags.SequenceEqual(other.Tags);

        public override int GetHashCode()
            => HashCode.Combine(this.StartIndex, this.EndIndex, this.Tags);
    }
}
