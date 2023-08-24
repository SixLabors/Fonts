// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace UnicodeTrieGenerator.StateAutomation
{
    internal static class Compile
    {
        public static StateMachine Build(string value, IDictionary<string, int>? externalSymbols = null)
        {
            SymbolTable symbolTable = Parse(value, externalSymbols);
            return Build(symbolTable);
        }

        private static SymbolTable Parse(string value, IDictionary<string, int>? externalSymbols = null)
        {
            IList<INode> ast = new GrammarParser().Parse(value);
            return new SymbolTable(ast, new(externalSymbols ?? new Dictionary<string, int>()));
        }

        private static StateMachine Build(SymbolTable symbolTable)
        {
            ILogicalNode main = symbolTable.Main();
            IEnumerable<State> state = DeterministicFiniteAutomata.Build(main, symbolTable.Size);

            int[][] stateTable = state.Select(x => x.Transitions).ToArray();
            bool[] accepting = state.Select(x => x.Accepting).ToArray();
            string[][] tags = state.Select(x => x.Tags.ToArray()).ToArray();
            return new StateMachine(stateTable, accepting, tags);
        }
    }
}
