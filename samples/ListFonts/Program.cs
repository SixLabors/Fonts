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
            var families = FontCollection.SystemFonts.Families;
            var orderd = families.OrderBy(x => x.Name);
            var len = families.Max(x => x.Name.Length);
            foreach (var f in orderd)
            {
                Console.Write(f.Name.PadRight(len));
                Console.Write('\t');
                Console.Write(string.Join(",", f.AvailibleStyles.OrderBy(x=>x).Select(x => x.ToString())));
                Console.WriteLine();
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