// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using SixLabors.Fonts;

namespace ListFonts;

public static class Program
{
    public static void Main(string[] args)
    {
        List<Pairing> pairings = new();
        IOrderedEnumerable<FontFamily> ordered = SystemFonts.Families.OrderBy(x => x.Name);
        foreach (FontFamily family in ordered)
        {
            IOrderedEnumerable<FontStyle> styles = family.GetAvailableStyles().OrderBy(x => x);
            foreach (FontStyle style in styles)
            {
                Font font = family.CreateFont(0F, style);
                font.TryGetPath(out string path);
                pairings.Add(new Pairing(font.Name, path));
            }
        }

        int max = pairings.Max(x => x.Name.Length);
        foreach (Pairing p in pairings)
        {
            Console.WriteLine($"{p.Name.PadRight(max)} {p.Path}");
        }

        if (Debugger.IsAttached)
        {
            Console.WriteLine(string.Empty);
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(100);
            }
        }
    }

    public struct Pairing
    {
        public Pairing(string name, string path)
        {
            this.Name = name;
            this.Path = path;
        }

        public string Name { get; }

        public string Path { get; }
    }
}
