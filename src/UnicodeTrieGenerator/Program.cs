// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace UnicodeTrieGenerator
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("Generating Trie Data");
            Generator.GenerateLineBreakTrie();
            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
