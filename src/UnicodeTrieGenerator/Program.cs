// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace UnicodeTrieGenerator;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Trie Data");
        Generator.GenerateUnicodeTries();
        Console.WriteLine("Done");
        Console.ReadLine();
    }
}
