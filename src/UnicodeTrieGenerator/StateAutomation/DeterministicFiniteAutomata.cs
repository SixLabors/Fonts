// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#nullable enable


namespace UnicodeTrieGenerator.StateAutomation;

/// <summary>
/// This is an implementation of the direct regular expression to DFA algorithm described
/// in section 3.9.5 of "Compilers: Principles, Techniques, and Tools" by Aho,
/// Lam, Sethi, and Ullman. <see href="http://dragonbook.stanford.edu"/>
/// There is a PDF of the book here:
/// <see href="http://web.archive.org/web/20131126083344/http://www.informatik.uni-bremen.de/agbkb/lehre/ccfl/Material/ALSUdragonbook.pdf"/>
/// </summary>
internal static class DeterministicFiniteAutomata
{
    internal static readonly EndMarker EndMarker = new();

    public static IEnumerable<State> Build(ILogicalNode root, int numSymbols)
    {
        root = new Concatenation(root, EndMarker);
        root.CalcFollowPos();

        State failState = new(new HashSet<INode>(), numSymbols);
        State initialState = new(root.FirstPos, numSymbols);

        List<State> dstates = new() { failState, initialState };

        // While there is an unmarked state S in dstates
        while (true)
        {
            State? s = null;

            for (int i = 1; i < dstates.Count; i++)
            {
                if (!dstates[i].Marked)
                {
                    s = dstates[i];
                    break;
                }
            }

            if (s is null)
            {
                break;
            }

            // Mark S
            s.Marked = true;

            // For each input symbol a
            for (int a = 0; a < numSymbols; a++)
            {
                // let U be the union of followpos(p) for all
                //  p in S that correspond to a
                HashSet<INode> u = new();
                foreach (INode p in s.Positions)
                {
                    if (p is Literal l && l.Value == a)
                    {
                        NodeUtilities.AddAll(u, p.FollowPos);
                    }
                }

                if (u.Count == 0)
                {
                    continue;
                }

                // if U is not in dstates
                int ux = -1;
                for (int i = 0; i < dstates.Count; i++)
                {
                    if (NodeUtilities.Equal(u, dstates[i].Positions))
                    {
                        ux = i;
                        break;
                    }
                }

                if (ux == -1)
                {
                    // Add U as an unmarked state to dstates
                    dstates.Add(new State(u, numSymbols));
                    ux = dstates.Count - 1;
                }

                s.Transitions[a] = ux;
            }
        }

        return dstates;
    }
}
