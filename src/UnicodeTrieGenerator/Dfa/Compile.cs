// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using SixLabors.Fonts;

namespace UnicodeTrieGenerator.Dfa
{
    internal static class Compile
    {
        // TODO: Should external symbols be params?
        public static SymbolTable Parse(string value, IEnumerable<KeyValuePair<string, int>>? externalSymbols = null)
        {
            IList<INode> ast = new GrammarParser().Parse(value);
            return new SymbolTable(ast, new(externalSymbols ?? Array.Empty<KeyValuePair<string, int>>()));
        }

        public static StateMachine Build(SymbolTable symbolTable)
        {
            IEnumerable<State> state = Dfa.BuildDfa(symbolTable.Main(), symbolTable.Size);

            // TODO: Complete state machine.
            return new StateMachine();
        }
    }
}
