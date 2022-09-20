// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using SixLabors.Fonts;

namespace UnicodeTrieGenerator.Dfa
{
    internal static class Compile
    {
        public static SymbolTable Parse(string value, IEnumerable<KeyValuePair<string, int>>? externalSymbols = null)
        {
            IList<INode> ast = new GrammarParser().Parse(value);
            return new SymbolTable(ast, externalSymbols ?? Array.Empty<KeyValuePair<string, int>>());
        }
    }
}
