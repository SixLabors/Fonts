// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace UnicodeTrieGenerator.StateAutomation;

internal sealed class State
{
    public State(ICollection<INode> positions, int length)
    {
        this.Positions = positions;
        this.Transitions = new int[length];
        this.Accepting = positions.Any(x => x == DeterministicFiniteAutomata.EndMarker);
        this.Marked = false;
        this.Tags = new HashSet<string>();
        foreach (INode pos in positions)
        {
            if (pos is Tag tag)
            {
                this.Tags.Add(tag.Name);
            }
        }
    }

    public ICollection<INode> Positions { get; set; }

    public int[] Transitions { get; }

    public bool Marked { get; set; }

    public bool Accepting { get; }

    public ICollection<string> Tags { get; set; }
}
