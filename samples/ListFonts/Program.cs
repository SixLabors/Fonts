using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SixLabors.Fonts;

namespace ListFonts
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Collections.Generic.IEnumerable<FontFamily> families = SystemFonts.Families;
            IOrderedEnumerable<FontFamily> orderd = families.OrderBy(x => x.Name);
            int len = families.Max(x => x.Name.Length);
            foreach (FontFamily f in orderd)
            {
                Console.Write(f.Name.PadRight(len));
                Console.Write('\t');
                Console.Write(string.Join(",", f.AvailableStyles.OrderBy(x=>x).Select(x => x.ToString())));
                Console.WriteLine();

                var g = f.CreateFont(10).Instance.GetGlyph(1);
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("");
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
