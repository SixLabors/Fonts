// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;

namespace SixLabors.Fonts.Unicode.Dfa
{
    internal static class Compile
    {
        // TODO: Should external symbols be params?
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
            IEnumerable<State> state = Dfa.BuildDfa(symbolTable.Main(), symbolTable.Size);

            int[][] stateTable = state.Select(x => x.Transitions).ToArray();
            bool[] accepting = state.Select(x => x.Accepting).ToArray();
            ICollection<string>[] tags = state.Select(x => x.Tags).ToArray();
            return new StateMachine(stateTable, accepting, tags);
        }
    }
}
