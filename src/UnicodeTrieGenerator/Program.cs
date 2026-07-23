// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using UnicodeTrieGenerator;

Console.WriteLine("Generating Trie Data");
Generator.GenerateUnicodeTries();
Console.WriteLine("Generating OpenType Language Tag Map");
Generator.GenerateOpenTypeLanguageTagMap();
Console.WriteLine("Done");
Console.ReadLine();
